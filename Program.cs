using MyCompany.Integrations.VaultPlugin;

var builder = WebApplication.CreateBuilder(args);

// Setup logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Register Vault service
builder.Services.Configure<VaultConfig>(builder.Configuration.GetSection(nameof(VaultConfig)));
builder.Services.AddSingleton<VaultService>();

// Add Web API
builder.Services.AddControllers();

var app = builder.Build();

// Configure middleware
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("HashiCorp Vault Credential Injection Service starting...");
logger.LogInformation("Service running on {Urls}", string.Join(", ", app.Urls));

await app.RunAsync();
