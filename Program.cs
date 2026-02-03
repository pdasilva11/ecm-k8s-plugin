using BeyondTrust.Integration.ECM.SDK;
using BeyondTrust.Integration.ECM.SDK.Model;
using BeyondTrust.Integration.ECM.SDK.Plugin;
using BeyondTrust.Integration.ECM.SDK.Util;
using MyCompany.Integrations.VaultPlugin;

var builder = Host.CreateApplicationBuilder(args);

// =========================================================================================================================================
// Step #1: Setup logging and initialize the logger factory
// -----------------------------------------------------------------------------------------------------------------------------------------
builder.Logging.ClearProviders();
builder.Logging.AddConsole();  // Console logging for container output

// Initialize the ECM logger factory
builder.Services.AddSingleton<ECMLoggerFactory>();
builder.Services.BuildServiceProvider().GetService<ECMLoggerFactory>();

var _logger = ECMLoggerFactory.CreateLogger<Program>();
_logger.LogInformation("Initializing HashiCorp Vault ECM Plugin and starting ECM Service");
// -----------------------------------------------------------------------------------------------------------------------------------------

// =========================================================================================================================================
// Step #2: Load plugin configuration and add the plugin
// -----------------------------------------------------------------------------------------------------------------------------------------
builder.Services.Configure<VaultPluginConfig>(builder.Configuration.GetSection(nameof(VaultPluginConfig)));
builder.Services.AddSingleton<IECMPlugin, VaultECMPlugin>();
_logger.LogInformation("Vault ECM Plugin configured");
// -----------------------------------------------------------------------------------------------------------------------------------------

// =========================================================================================================================================
// Step #3: Load ECM service configuration and add the service
// -----------------------------------------------------------------------------------------------------------------------------------------
builder.Services.Configure<ECMConfig>(builder.Configuration.GetSection(nameof(ECMConfig)));
builder.Services.AddSingleton<ECMServiceFactory>();
builder.Services.AddHostedService(sp =>
{
    _logger.LogInformation("Creating ECM Service to connect to PRA");
    return builder.Services.BuildServiceProvider()
        .GetService<ECMServiceFactory>()!
        .CreateECMService();
});
// -----------------------------------------------------------------------------------------------------------------------------------------

_logger.LogInformation("Starting HashiCorp Vault ECM Plugin Host");

var host = builder.Build();
await host.RunAsync();
