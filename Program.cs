using BeyondTrust.Integration.ECM.SDK;
using BeyondTrust.Integration.ECM.SDK.Model;
using BeyondTrust.Integration.ECM.SDK.Plugin;
using BeyondTrust.Integration.ECM.SDK.Util;
using MyCompany.Integrations.ExamplePlugin;
using System.Runtime.InteropServices;

// Disable console mode when running in container to prevent Console.ReadKey() errors
if (!Console.IsInputRedirected || Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
{
    Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", "true");
}

var builder = Host.CreateApplicationBuilder(args);

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "BeyondTrust Example ECM Service";
    });
}

// =========================================================================================================================================
// Step #1: Setup logging and initialize the logger factory so it's available when other services and classes are instantiated
// -----------------------------------------------------------------------------------------------------------------------------------------
builder.Logging.ClearProviders();  // Clear the default providers if desired, then add the provider(s) you wish to use

/***** Core logging (Microsoft.Extensions.Logging) *****/
builder.Logging.AddConsole();  // Core console logging capabilities if you might run in console mode

/***** Log4Net *****
 * There are multiple packages that provide a wrapper for log4net; pick your favorite and use / inject it according to the package docs
 * This is just one example where the package provides an extension method, AddLog4Net(), for ILoggingBuilder
 * NOTE: Additional packages may be required depending on how you configure and use it
 * 
  builder.Logging.AddLog4Net(
      log4NetConfigFile: "log4net.config",
      watch: true);
*/

/***** Serilog *****
 * There are multiple packages that provide a wrapper for serilog; pick your favorite and use / inject it according to the package docs
 * This is just one example where the package provides an extension method, AddSerilog(), for ILoggingBuilder
 * NOTE: Additional packages may be required depending on how you configure and use it
 * 
  Log.Logger = new LoggerConfiguration()
      .ReadFrom.Configuration(builder.Configuration)
      .CreateLogger();
  builder.Logging.AddSerilog();
*/

// Now that our core factory is loaded with the desired providers and configuration, initialize the factory that will provide loggers
// for all the types within this project as well as the ECM itself
builder.Services.AddSingleton<ECMLoggerFactory>();
// This line is critical because it forces the DI container to initialize the logging BEFORE any classes / services are initialized
builder.Services.BuildServiceProvider().GetService<ECMLoggerFactory>();

var _logger = ECMLoggerFactory.CreateLogger<Program>();  // Create a logger to be used here in this class
_logger.LogInformation("Initializing Template Plugin and starting ECM Service");
// -----------------------------------------------------------------------------------------------------------------------------------------

// =========================================================================================================================================
// Step #2: Load plugin configuration and add the plugin (implements IECMPlugin). This can come from anywhere - the appsettings.json
// file, a database, cmd line args, environment vars, etc.
//   -- For this example, we'll rely on the appsettings.json file ... it is NOT secure, but it is simple. In a real world scenario
//   -- you would want to ensure that your configuration (particularly any sensitive info) is stored securely. See other examples below.
// -----------------------------------------------------------------------------------------------------------------------------------------
builder.Services.Configure<ExamplePluginConfig>(builder.Configuration.GetSection(nameof(ExamplePluginConfig)));  // from appsettings.json

//***** Database *****
//builder.Services.AddOptions<ExamplePluginConfig>()
//    .Bind()


/***** Azure Key Vault *****
 * This is just an example that uses Azure.Extensions.AspNetCore.Configuration.Secrets. You may wish to use another and usage may vary
 * as the package is updated or your specific use case differs.
 * 
  var azureConfig = new ConfigurationBuilder()
      .AddAzureKeyVault(new Uri("<Vault URI>"), azureCredential)
      .Build();
  builder.Services.Configure<ExamplePluginConfig>(azureConfig);
*/

builder.Services.AddSingleton<IECMPlugin, ExamplePlugin>();  // Now that configuration is loaded, add the IECMPlugin implementation
// -----------------------------------------------------------------------------------------------------------------------------------------

// =========================================================================================================================================
// Step #3: Load service configuration and add the service. Again, we'll stick with appsettings.json for now but swap for any other source
// -----------------------------------------------------------------------------------------------------------------------------------------
builder.Services.Configure<ECMConfig>(builder.Configuration.GetSection(nameof(ECMConfig)));
builder.Services.AddSingleton<ECMServiceFactory>();
builder.Services.AddHostedService(sp =>
{
    return builder.Services.BuildServiceProvider()
        .GetService<ECMServiceFactory>()
        .CreateECMService();
});
// -----------------------------------------------------------------------------------------------------------------------------------------

// =====================================================================================================================================
// Step #4: Conditionally setup the process to work with the Service Control Manager when running as a Windows Service
// -------------------------------------------------------------------------------------------------------------------------------------
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = builder.Services.BuildServiceProvider().GetService<IECMPlugin>().Name;
    });
}
// -------------------------------------------------------------------------------------------------------------------------------------

// =====================================================================================================================================
// Step #5: Build and run the host
// -------------------------------------------------------------------------------------------------------------------------------------
var host = builder.Build();
await host.RunAsync();
// -------------------------------------------------------------------------------------------------------------------------------------
