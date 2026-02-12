using Microsoft.EntityFrameworkCore;
using SistemaVendasDoces.Data;

var builder = WebApplication.CreateBuilder(args);

// Configuração de Porta para Railway (PRIORIDADE MÁXIMA)
// O Railway define a porta na variável PORT. Precisamos escutar 0.0.0.0:PORT
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configuração do banco de dados MySQL
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

if (string.IsNullOrEmpty(connectionString))
{
    // Local development
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
}
else
{
    // Fix para Railway: Converter URL mysql:// para connection string padrão
    try 
    {
        var dbUri = new Uri(connectionString);
        var userInfo = dbUri.UserInfo.Split(new[] { ':' }, 2);
        var username = userInfo[0];
        var password = userInfo.Length > 1 ? userInfo[1] : "";
        password = Uri.UnescapeDataString(password);

        var builderStr = new MySqlConnector.MySqlConnectionStringBuilder
        {
            Server = dbUri.Host,
            Port = (uint)dbUri.Port,
            Database = dbUri.AbsolutePath.TrimStart('/'),
            UserID = username,
            Password = password,
            SslMode = MySqlConnector.MySqlSslMode.None, // Garantir compatibilidade total
            AllowPublicKeyRetrieval = true // Necessário para alguns servidores MySQL
        };

        connectionString = builderStr.ConnectionString;
        
        // Log para debug (sem mostrar a senha)
        Console.WriteLine($"[DEBUG] Conectando em: Server={builderStr.Server};Port={builderStr.Port};Database={builderStr.Database};User={builderStr.UserID};SslMode={builderStr.SslMode}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro ao fazer parse da DATABASE_URL: {ex.Message}");
    }
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 36)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)
    ));

var app = builder.Build();

// Auto-migrate database on startup
// BLOCO TRY-CATCH para evitar que o app caia se o banco falhar (permite ver logs)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine("------------------------------------------");
        Console.WriteLine("ERRO CRÍTICO NA MIGRAÇÃO DO BANCO DE DADOS:");
        Console.WriteLine(ex.Message);
        Console.WriteLine("------------------------------------------");
        // Não relança a exceção para permitir que a aplicação suba e responda ao Health Check
    }
}

// Configuração de Localização (pt-BR)
var supportedCultures = new[] { "pt-BR" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

// Configure the HTTP request pipeline.
// if (!app.Environment.IsDevelopment())
// {
//     app.UseExceptionHandler("/Home/Error");
// }
app.UseDeveloperExceptionPage(); // DEBUG: Forçar exibição do erro no Railway

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

