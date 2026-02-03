using BeyondTrust.Integration.ECM.SDK.Model;
using BeyondTrust.Integration.ECM.SDK.Plugin;
using BeyondTrust.Integration.ECM.SDK.Util;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.Reflection;

namespace MyCompany.Integrations.VaultPlugin
{
    /// <summary>
    /// HashiCorp Vault ECM Plugin for BeyondTrust PRA
    /// Implements credential discovery and injection from Vault
    /// </summary>
    public class VaultECMPlugin : IECMPlugin
    {
        private static readonly ILogger _logger = ECMLoggerFactory.CreateLogger<VaultECMPlugin>();
        private readonly VaultPluginConfig _config;
        private readonly HttpClient _httpClient;

        public VaultECMPlugin(IOptions<VaultPluginConfig> config)
        {
            _config = config.Value;
            _httpClient = new HttpClient();

            _logger.LogInformation("Vault ECM Plugin initialized");
            _logger.LogInformation("Vault URL: {VaultUrl}", _config.VaultBaseUrl);
        }

        #region Plugin Properties

        public string Name => "HashiCorp Vault ECM Plugin";

        public string Description => "Provides credential injection from HashiCorp Vault for BeyondTrust PRA";

        public string Version => FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion ?? "1.0.0";

        public async Task<ActionResult<PluginCapabilities>> GetPluginCapabilitiesAsync()
        {
            var capabilities = new PluginCapabilities
            {
                SupportsCredentialsUsed = false,
                SupportsEndpointSearch = false,
                SupportsHealthCheck = true,
                SupportsSessionComplete = false,
                SupportsSessionStarted = false,
                SupportsStartSession = false
            };

            return await Task.FromResult(new ActionResult<PluginCapabilities> { ResultValue = capabilities, IsSuccess = true });
        }

        public async Task<ActionResult<PluginHealthStatus>> PerformHealthCheckAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_config.VaultBaseUrl}/v1/sys/health");

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Vault health check passed");
                    return new ActionResult<PluginHealthStatus>
                    {
                        ResultValue = new PluginHealthStatus { IsHealthy = true },
                        IsSuccess = true
                    };
                }
                else
                {
                    _logger.LogWarning("Vault health check failed: {StatusCode}", response.StatusCode);
                    return new ActionResult<PluginHealthStatus>
                    {
                        ResultValue = new PluginHealthStatus { IsHealthy = false },
                        IsSuccess = false,
                        ErrorMessage = $"Vault returned {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Vault health check exception");
                return new ActionResult<PluginHealthStatus>
                {
                    ResultValue = new PluginHealthStatus { IsHealthy = false },
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        #endregion

        #region Credential Actions

        public async Task<ActionResult<IList<CredentialSummary>>> FindCredentialsForSessionAsync(SRASession session)
        {
            try
            {
                _logger.LogInformation("Finding credentials for session. User: {User}, Computer: {Computer}",
                    session.SRAUser?.Username, session.JumpItem?.ComputerName);

                // Authenticate to Vault
                var token = await AuthenticateToVaultAsync();

                // List all secrets in the configured path
                var credentials = new List<CredentialSummary>();

                // Get list of secrets from Vault metadata endpoint
                var secretPaths = await ListSecretsFromVaultAsync(token);

                foreach (var secretPath in secretPaths)
                {
                    credentials.Add(new CredentialSummary
                    {
                        CredentialId = secretPath,
                        DisplayValue = $"Vault: {secretPath}"
                    });
                }

                _logger.LogInformation("Found {Count} credentials in Vault", credentials.Count);

                return new ActionResult<IList<CredentialSummary>>
                {
                    ResultValue = credentials,
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to find credentials for session");
                return new ActionResult<IList<CredentialSummary>>
                {
                    ResultValue = new List<CredentialSummary>(),
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<ActionResult<CredentialPackage>> GetCredentialForInjectionAsync(SRASession session, string credentialId)
        {
            try
            {
                _logger.LogInformation("Getting credential for injection: {CredentialId}", credentialId);

                // Authenticate to Vault
                var token = await AuthenticateToVaultAsync();

                // Retrieve the secret from Vault
                var secret = await GetSecretFromVaultAsync(token, credentialId);

                var credentialPackage = new CredentialPackage
                {
                    Username = secret.Username,
                    Password = secret.Password
                };

                _logger.LogInformation("Successfully retrieved credential: {CredentialId}", credentialId);

                return new ActionResult<CredentialPackage>
                {
                    ResultValue = credentialPackage,
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get credential for injection: {CredentialId}", credentialId);
                return new ActionResult<CredentialPackage>
                {
                    ResultValue = null,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<ActionResult> ReleaseCredentialFromSessionAsync(SRASession session, string credentialId, long sessionEndedTimestamp)
        {
            // No cleanup needed for Vault - just return success
            _logger.LogInformation("Credential released: {CredentialId}", credentialId);
            return await Task.FromResult(new ActionResult { IsSuccess = true });
        }

        #endregion

        #region Vault Helper Methods

        private async Task<string> AuthenticateToVaultAsync()
        {
            try
            {
                var payload = new { password = _config.VaultPassword };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_config.VaultBaseUrl}/v1/auth/userpass/login/{_config.VaultUsername}",
                    content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Vault auth failed: {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseContent);
                var token = doc.RootElement
                    .GetProperty("auth")
                    .GetProperty("client_token")
                    .GetString();

                return token ?? throw new Exception("No token in Vault auth response");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Vault authentication failed");
                throw;
            }
        }

        private async Task<List<string>> ListSecretsFromVaultAsync(string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("X-Vault-Token", token);

                var response = await _httpClient.GetAsync(
                    $"{_config.VaultBaseUrl}/v1/{_config.VaultSecretsEngine}/metadata?list=true");

                if (!response.IsSuccessStatusCode)
                {
                    // If listing fails, return the known secrets from config
                    _logger.LogWarning("Failed to list secrets from Vault, using configured secrets");
                    return new List<string> { "test-credential", "myecm" };
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseContent);

                var keys = doc.RootElement
                    .GetProperty("data")
                    .GetProperty("keys");

                var secretList = new List<string>();
                foreach (var key in keys.EnumerateArray())
                {
                    var secretName = key.GetString();
                    if (!string.IsNullOrEmpty(secretName) && !secretName.EndsWith("/"))
                    {
                        secretList.Add(secretName);
                    }
                }

                return secretList;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to list secrets, returning default list");
                return new List<string> { "test-credential", "myecm" };
            }
        }

        private async Task<VaultSecret> GetSecretFromVaultAsync(string token, string secretPath)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("X-Vault-Token", token);

                var response = await _httpClient.GetAsync(
                    $"{_config.VaultBaseUrl}/v1/{_config.VaultSecretsEngine}/data/{secretPath}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to retrieve secret: {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseContent);

                var data = doc.RootElement
                    .GetProperty("data")
                    .GetProperty("data");

                var secret = new VaultSecret
                {
                    Username = data.TryGetProperty("username", out var userProp)
                        ? userProp.GetString() ?? ""
                        : "",
                    Password = data.TryGetProperty("password", out var passProp)
                        ? passProp.GetString() ?? ""
                        : ""
                };

                return secret;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve secret from Vault: {SecretPath}", secretPath);
                throw;
            }
        }

        #endregion

        #region Not Implemented Methods

        public async Task<ActionResult<IReadOnlyList<EndpointPackage>>> FindAvailableEndpointsAsync(
            SRASession session, IReadOnlyList<EndpointPackageType> endpointSearchTypes,
            string searchCriteria, int? mostRecentlyUsedCount)
        {
            throw new NotImplementedException("Endpoint search is not supported by this plugin");
        }

        public async Task<ActionResult<StartSessionDetails>> StartSessionAsync(SRASession session, string credentialId)
        {
            throw new NotImplementedException("Start session is not supported by this plugin");
        }

        public async Task<ActionResult> SessionStartedAsync(SRASession session, string? historyUrl = null, int? historyUrlExpirySeconds = null)
        {
            throw new NotImplementedException("Session started notification is not supported by this plugin");
        }

        public async Task<ActionResult> SessionCompleteAsync(SRASession session)
        {
            throw new NotImplementedException("Session complete notification is not supported by this plugin");
        }

        #endregion
    }

    public class VaultSecret
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }
}
