using Microsoft.EntityFrameworkCore;
using SistemaVendasDoces.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configuração do banco de dados MySQL
// No Railway: adicionar variável DATABASE_URL com a connection string
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

if (string.IsNullOrEmpty(connectionString))
{
    // Local development - usa appsettings.json
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
}
else
{
    // Fix para Railway: Converter URL mysql:// para connection string padrão
    // Formato esperado: mysql://user:password@host:port/database
    try 
    {
        var dbUri = new Uri(connectionString);
        var userInfo = dbUri.UserInfo.Split(':');
        connectionString = $"Server={dbUri.Host};Port={dbUri.Port};Database={dbUri.AbsolutePath.TrimStart('/')};User={userInfo[0]};Password={userInfo[1]};";
    }
    catch (Exception)
    {
        // Se falhar o parse (já estiver no formato correto), mantém como está
        Console.WriteLine("Aviso: Não foi possível fazer o parse da DATABASE_URL como URI. Usando string original.");
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

// Auto-migrate database on startup (importante pra Railway)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Configuração de Localização (pt-BR)
var supportedCultures = new[] { "pt-BR" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Configuração de Porta para Railway (e outros serviços cloud)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    // Se a variável PORT estiver definida (ambiente de produção/cloud), escuta nessa porta
    app.Run($"http://0.0.0.0:{port}");
}
else
{
    // Desenvolvimento local
    app.Run();
}

