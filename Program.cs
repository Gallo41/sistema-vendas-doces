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

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 36))
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

app.Run();

