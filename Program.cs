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
// Tenta ler DATABASE_URL ou MYSQL_URL (Railway costuma usar MYSQL_URL para o plugin)
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
if (string.IsNullOrEmpty(connectionString)) connectionString = Environment.GetEnvironmentVariable("MYSQL_URL");

// DETECÇÃO DE AMBIENTE: Se tem PORT definido, é Railway (ou outro container)
var isRailway = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PORT"));

if (isRailway && string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("[EMERGENCY] Variáveis de ambiente falharam. Usando credenciais de EMERGÊNCIA para salvar o deploy!");
    // Credenciais extraídas do print do usuário - usando URL PÚBLICA (proxy) pois a interna não resolve
    connectionString = "Server=amabiko.proxy.rlwy.net;Port=48605;Database=railway;Uid=root;Pwd=htBSQtCKGznMPKayosIjXHMl0yNbQwbTA;Protocol=Tcp;SslMode=None";
}

if (!isRailway && string.IsNullOrEmpty(connectionString))
{
    // Local development
    Console.WriteLine("[DEBUG] Rodando em ambiente LOCAL (ou variáveis de banco vazias)");
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
}
else if (!string.IsNullOrEmpty(connectionString) && !connectionString.Contains("Uid=root")) // Evitar re-processar a string hardcoded que já está no formato correto
{
    Console.WriteLine("[DEBUG] Rodando em RAILWAY. Conexão encontrada/definida!");
    // Fix para Railway: Converter URL mysql:// para connection string padrão
    try 
    {
        // Se já for uma connection string padrão (não URL), pula o parse
        if (!connectionString.StartsWith("mysql://")) 
        {
             Console.WriteLine("[DEBUG] Connection string já está no formato padrão.");
        }
        else 
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
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[CRITICAL] Erro ao fazer parse da URL do banco: {ex.Message}");
    }
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 36)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null)
    ));

var app = builder.Build();

// Auto-migrate database on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        // Tenta rodar a migração. Se falhar, loga o erro mas deixa o app subir.
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
        Console.WriteLine("[SUCCESS] Migração do banco realizada com sucesso!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[IGNORED] Erro na inicialização do DB (o app vai subir mesmo assim): {ex.Message}");
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
    var mysqlUrl = Environment.GetEnvironmentVariable("MYSQL_URL");
    
    sb.AppendLine($"DATABASE_URL: {(string.IsNullOrEmpty(envStr) ? "Não Encontrada" : "Encontrada")}");
    sb.AppendLine($"MYSQL_URL: {(string.IsNullOrEmpty(mysqlUrl) ? "Não Encontrada" : "Encontrada")}");

    var connectionStringToTest = "";

    // Tentar processar MYSQL_URL se existir
    if (!string.IsNullOrEmpty(mysqlUrl))
    {
        sb.AppendLine("\nProcessando MYSQL_URL:");
        try {
            var uri = new Uri(mysqlUrl);
            sb.AppendLine($" - Host: {uri.Host}");
            sb.AppendLine($" - Port: {uri.Port}");
            sb.AppendLine($" - Scheme: {uri.Scheme}");
            sb.AppendLine($" - User Info: {(string.IsNullOrEmpty(uri.UserInfo) ? "Vazio" : "***")}");
            
            var userInfo = uri.UserInfo.Split(new[] { ':' }, 2);
            var username = userInfo[0];
            var password = userInfo.Length > 1 ? userInfo[1] : "";
            password = Uri.UnescapeDataString(password);
            
            var headerPort = uri.Port;
            if (headerPort == -1) headerPort = 3306;

            var builderStr = new MySqlConnector.MySqlConnectionStringBuilder
            {
                Server = uri.Host,
                Port = (uint)headerPort,
                Database = uri.AbsolutePath.TrimStart('/'),
                UserID = username,
                Password = password,
                SslMode = MySqlConnector.MySqlSslMode.None,
                AllowPublicKeyRetrieval = true,
                ConnectionProtocol = MySqlConnector.MySqlConnectionProtocol.Tcp 
            };
            connectionStringToTest = builderStr.ConnectionString;
            sb.AppendLine($" -> Connection String montada com sucesso: Server={builderStr.Server}");
        } catch (Exception ex) {
            sb.AppendLine($"ERRO AO FAZER PARSE DA MYSQL_URL: {ex.Message}");
        }
    }
    else if (!string.IsNullOrEmpty(envStr))
    {
         // Lógica para DATABASE_URL se existisse...
    }

    // Check for placeholders
    var hasPlaceholders = false;
    foreach (System.Collections.DictionaryEntry env in Environment.GetEnvironmentVariables())
    {
        var val = env.Value?.ToString() ?? "";
        if (val.StartsWith("{{") && val.EndsWith("}}"))
        {
            hasPlaceholders = true;
            sb.AppendLine($"[AVISO] A variável {env.Key} contém um placeholder não substituído: {val}");
        }
    }

    if (hasPlaceholders)
    {
        sb.AppendLine("\n[ERRO CRÍTICO] O Railway não substituiu as variáveis de ambiente!");
        sb.AppendLine("-> Solução: Vá nas configurações do Railway e adicione manualmente a variável DATABASE_URL com a connection string completa.");
    }
    
    sb.AppendLine("\nVariáveis de Ambiente (MYSQL* + PORT):");
    foreach (System.Collections.DictionaryEntry env in Environment.GetEnvironmentVariables())
    {
        var key = env.Key.ToString();
        if (key.ToUpper().Contains("MYSQL") || key.ToUpper().Contains("DB") || key.ToUpper().Contains("PORT"))
        {
             var val = env.Value?.ToString() ?? "";
             var displayVal = val;
             
             // Mascarar senha ou URL longa
             if (key.ToUpper().Contains("URL") || key.ToUpper().Contains("PASS")) 
             {
                 if (string.IsNullOrEmpty(val)) displayVal = "(VAZIO)";
                 else if (val.StartsWith("{{")) displayVal = val; // Mostrar placeholder
                 else displayVal = val.Length > 10 ? val.Substring(0, 10) + "..." : "***";
             }
             
             sb.AppendLine($"- {key} = {displayVal}");
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
            sb.AppendLine($"String de Conexão usada (host): {new MySqlConnector.MySqlConnectionStringBuilder(connectionString).Server}");
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

