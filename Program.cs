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
var isRailway = !string.IsNullOrEmpty(connectionString);

if (!isRailway)
{
    // Local development
    Console.WriteLine("[DEBUG] Rodando em ambiente LOCAL (ou DATABASE_URL vazia)");
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
}
else
{
    Console.WriteLine("[DEBUG] Rodando em RAILWAY. Processando DATABASE_URL...");
    // Fix para Railway: Converter URL mysql:// para connection string padrão
    try 
    {
        var dbUri = new Uri(connectionString);
        var userInfo = dbUri.UserInfo.Split(new[] { ':' }, 2);
        var username = userInfo[0];
        var password = userInfo.Length > 1 ? userInfo[1] : "";
        password = Uri.UnescapeDataString(password);

        var headerPort = dbUri.Port;
        if (headerPort == -1) headerPort = 3306;

        var builderStr = new MySqlConnector.MySqlConnectionStringBuilder
        {
            Server = dbUri.Host,
            Port = (uint)headerPort,
            Database = dbUri.AbsolutePath.TrimStart('/'),
            UserID = username,
            Password = password,
            SslMode = MySqlConnector.MySqlSslMode.None,
            AllowPublicKeyRetrieval = true,
            ConnectionProtocol = MySqlConnector.MySqlConnectionProtocol.Tcp 
        };

        connectionString = builderStr.ConnectionString;
        
        Console.WriteLine($"[DEBUG] Conectando em (TCP): Server={builderStr.Server};Port={builderStr.Port};User={builderStr.UserID};Ssl={builderStr.SslMode}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[CRITICAL] Erro ao fazer parse da DATABASE_URL: {ex.Message}");
    }
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 36)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 2, // Reduzido para testar rápido
            maxRetryDelay: TimeSpan.FromSeconds(2),
            errorNumbersToAdd: null)
    ));

var app = builder.Build();

// Auto-migrate database on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        // ATENÇÃO: Migração comentada para DEBUG. Se o banco não conectar, o app SOBE mesmo assim.
        // var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // db.Database.Migrate();
        Console.WriteLine("[DEBUG] Migração PULADA para garantir startup do container.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[IGNORED] Erro na inicialização do DB: {ex.Message}");
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

// Endpoint de DEBUG para diagnóstico do Railway
app.MapGet("/debug-railway", async (ApplicationDbContext db) =>
{
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("=== DIAGNÓSTICO RAILWAY ===");
    sb.AppendLine($"Data/Hora: {DateTime.Now}");
    
    var envStr = Environment.GetEnvironmentVariable("DATABASE_URL");
    sb.AppendLine($"DATABASE_URL existe? {!string.IsNullOrEmpty(envStr)}");
    
    if (!string.IsNullOrEmpty(envStr))
    {
        try {
            var uri = new Uri(envStr);
            sb.AppendLine($"Parse URI sucesso:");
            sb.AppendLine($" - Host: {uri.Host}");
            sb.AppendLine($" - Port: {uri.Port}");
            sb.AppendLine($" - Scheme: {uri.Scheme}");
        } catch (Exception ex) {
            sb.AppendLine($"ERRO AO FAZER PARSE DA URI: {ex.Message}");
        }
    }

    sb.AppendLine("\nVariáveis de Ambiente (MYSQL*):");
    foreach (System.Collections.DictionaryEntry env in Environment.GetEnvironmentVariables())
    {
        var key = env.Key.ToString();
        if (key.ToUpper().Contains("MYSQL") || key.ToUpper().Contains("DB") || key.ToUpper().Contains("PORT"))
        {
             var val = env.Value.ToString();
             // Mascarar senha
             if (key.ToUpper().Contains("URL") || key.ToUpper().Contains("PASS")) 
                 val = val.Length > 10 ? val.Substring(0, 10) + "..." : "***";
             
             sb.AppendLine($"- {key} = {val}");
        }
    }

    sb.AppendLine("\nTentando conexão com o banco...");
    try
    {
        if (db.Database.CanConnect())
        {
            sb.AppendLine("SUCESSO! Conexão estabelecida.");
        }
        else
        {
            sb.AppendLine("FALHA! CanConnect retornou false.");
        }
    }
    catch (Exception ex)
    {
        sb.AppendLine($"EXCEÇÃO ao conectar: {ex.Message}");
        if (ex.InnerException != null) sb.AppendLine($" - Inner: {ex.InnerException.Message}");
    }

    return Results.Text(sb.ToString());
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

