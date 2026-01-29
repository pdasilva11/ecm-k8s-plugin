using BeyondTrust.Integration.ECM.SDK.Model;
using BeyondTrust.Integration.ECM.SDK.Plugin;
using BeyondTrust.Integration.ECM.SDK.Util;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace MyCompany.Integrations.ExamplePlugin
{
    public class ExamplePlugin : IECMPlugin
    {
        private static readonly ILogger _logger = ECMLoggerFactory.CreateLogger<ExamplePlugin>();
        private readonly ExamplePluginConfig _config;

        // Vault configuration from environment variables
        private readonly string _vaultBaseUrl;
        private readonly string _vaultSecretsEngine;
        private readonly string _vaultUsername;
        private readonly string _vaultPassword;
        private readonly string _vaultEcmMap;

        public ExamplePlugin(IOptions<ExamplePluginConfig> config)
        {
            _config = config.Value;

            // Load Vault configuration from environment variables
            _vaultBaseUrl = Environment.GetEnvironmentVariable("VLT_BASE_URL") ?? "";
            _vaultSecretsEngine = Environment.GetEnvironmentVariable("VLT_SECRETS_ENGINE") ?? "";
            _vaultUsername = Environment.GetEnvironmentVariable("VLT_USERNAME") ?? "";
            _vaultPassword = Environment.GetEnvironmentVariable("VLT_PASSWORD") ?? "";
            _vaultEcmMap = Environment.GetEnvironmentVariable("VLT_ECM_MAP") ?? "";

            _logger.LogInformation("HashiCorp Vault ECM Plugin initialized");
            _logger.LogInformation("Vault URL: {VaultUrl}", _vaultBaseUrl);
        }

        #region General Properties and Methods

        public string Name => "BeyondTrust HashiCorp Vault ECM Plugin";

        public string Description => "ECM Plugin for retrieving credentials from HashiCorp Vault";

        public string Version => FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;

        public virtual async Task<ActionResult<PluginCapabilities>> GetPluginCapabilitiesAsync()
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

        public virtual async Task<ActionResult<PluginHealthStatus>> PerformHealthCheckAsync()
        {
            _logger.LogDebug("Performing health check");

            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(_vaultBaseUrl + "/v1/sys/health");

                var status = new PluginHealthStatus();
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Health check passed - Vault connection is healthy");
                }
                else
                {
                    _logger.LogWarning("Health check warning - Vault returned status {StatusCode}", response.StatusCode);
                }

                return new ActionResult<PluginHealthStatus>
                {
                    IsSuccess = true,
                    ResultValue = status
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check exception");
                return new ActionResult<PluginHealthStatus>
                {
                    IsSuccess = false,
                    ResultValue = new PluginHealthStatus()
                };
            }
        }

        #endregion

        #region Vault Authentication

        private async Task<string> GetAuthTokenAsync()
        {
            using var httpClient = new HttpClient();
            string jsonPayload = "{\"password\":\"" + _vaultPassword + "\"}";
            var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await httpClient.PostAsync(
                _vaultBaseUrl + "/v1/auth/userpass/login/" + _vaultUsername,
                httpContent);

            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Vault authentication failed: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);
                throw new Exception($"Vault authentication failed: {response.StatusCode}");
            }

            using var doc = JsonDocument.Parse(responseContent);
            var token = doc.RootElement.GetProperty("auth").GetProperty("client_token").GetString();

            _logger.LogDebug("Successfully authenticated to Vault");
            return token ?? throw new Exception("No token in Vault response");
        }

        private async Task<string> GetSecretAsync(string secretPath, string token)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("X-Vault-Token", token);

            var response = await httpClient.GetAsync(_vaultBaseUrl + "/v1/" + _vaultSecretsEngine + "/data/" + secretPath);
            return await response.Content.ReadAsStringAsync();
        }

        private async Task<List<EcmMapping>> GetEcmMappingsAsync(string token)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("X-Vault-Token", token);

            var response = await httpClient.GetAsync(_vaultBaseUrl + _vaultEcmMap);
            var content = await response.Content.ReadAsStringAsync();

            var mappings = new List<EcmMapping>();

            try
            {
                using var doc = JsonDocument.Parse(content);
                var data = doc.RootElement.GetProperty("data").GetProperty("data");

                foreach (var prop in data.EnumerateObject())
                {
                    var mapping = new EcmMapping
                    {
                        ComputerName = prop.Name,
                        Secret = prop.Value.GetString() ?? prop.Name
                    };
                    mappings.Add(mapping);
                    _logger.LogDebug("Found ECM mapping: {ComputerName} -> {Secret}", mapping.ComputerName, mapping.Secret);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing ECM mappings from Vault");
            }

            return mappings;
        }

        #endregion

        #region Credential Action Methods

        public async Task<ActionResult<IList<CredentialSummary>>> FindCredentialsForSessionAsync(SRASession session)
        {
            _logger.LogInformation("FindCredentialsForSessionAsync called");

            try
            {
                var token = await GetAuthTokenAsync();
                var targetMachine = session.JumpItem?.ComputerName ?? "";

                _logger.LogInformation("Looking for credentials for target: {Target}", targetMachine);

                var mappings = await GetEcmMappingsAsync(token);
                var credentials = new List<CredentialSummary>();

                foreach (var mapping in mappings)
                {
                    // Match if computer name matches or if mapping applies to all
                    if (string.IsNullOrEmpty(mapping.ComputerName) ||
                        mapping.ComputerName.Equals(targetMachine, StringComparison.OrdinalIgnoreCase) ||
                        mapping.ComputerName == "*")
                    {
                        credentials.Add(new CredentialSummary
                        {
                            CredentialId = mapping.Secret,
                            DisplayValue = "HashiCorp Vault: " + mapping.Secret
                        });

                        _logger.LogDebug("Added credential: {Secret} for {Target}", mapping.Secret, mapping.ComputerName);
                    }
                }

                _logger.LogInformation("Returning {Count} credentials for session", credentials.Count);

                return new ActionResult<IList<CredentialSummary>>
                {
                    IsSuccess = true,
                    ResultValue = credentials
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding credentials for session");
                return new ActionResult<IList<CredentialSummary>>
                {
                    IsSuccess = false,
                    ResultValue = new List<CredentialSummary>()
                };
            }
        }

        public async Task<ActionResult<CredentialPackage>> GetCredentialForInjectionAsync(SRASession session, string credentialId)
        {
            _logger.LogInformation("GetCredentialForInjectionAsync called for credential {CredentialId}", credentialId);

            try
            {
                var token = await GetAuthTokenAsync();
                var secretJson = await GetSecretAsync(credentialId, token);

                using var doc = JsonDocument.Parse(secretJson);
                var data = doc.RootElement.GetProperty("data").GetProperty("data");

                var username = data.GetProperty("username").GetString() ?? "";
                var password = data.GetProperty("password").GetString() ?? "";

                _logger.LogInformation("Successfully retrieved credential {CredentialId} for user {Username}",
                    credentialId, username);

                var credentialPackage = CredentialPackage.BuildUsernamePasswordPackage(credentialId, username, password);

                return new ActionResult<CredentialPackage>
                {
                    IsSuccess = true,
                    ResultValue = credentialPackage
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving credential {CredentialId}", credentialId);
                return new ActionResult<CredentialPackage>
                {
                    IsSuccess = false,
                    ResultValue = CredentialPackage.BuildUsernamePasswordPackage(credentialId, "", "")
                };
            }
        }

        public async Task<ActionResult> ReleaseCredentialFromSessionAsync(SRASession session, string credentialId, long sessionEndedTimestamp)
        {
            _logger.LogInformation("ReleaseCredentialFromSessionAsync called for credential {CredentialId}", credentialId);

            // HashiCorp Vault doesn't require explicit credential release
            _logger.LogInformation("Credential {CredentialId} released from session at {Timestamp}",
                credentialId, DateTimeOffset.FromUnixTimeSeconds(sessionEndedTimestamp));

            return await Task.FromResult(new ActionResult { IsSuccess = true });
        }

        #endregion

        #region Endpoint Action Methods

        public async Task<ActionResult<IReadOnlyList<EndpointPackage>>> FindAvailableEndpointsAsync(
            SRASession session,
            IReadOnlyList<EndpointPackageType> endpointSearchTypes,
            string searchCriteria,
            int? mostRecentlyUsedCount)
        {
            _logger.LogDebug("FindAvailableEndpointsAsync called - not implemented");

            return await Task.FromResult(new ActionResult<IReadOnlyList<EndpointPackage>>
            {
                IsSuccess = true,
                ResultValue = new List<EndpointPackage>()
            });
        }

        #endregion

        #region Session Action Methods

        public async Task<ActionResult<StartSessionDetails>> StartSessionAsync(SRASession session, string credentialId)
        {
            _logger.LogDebug("StartSessionAsync called - not implemented");
            // This method is not supported by this plugin
            throw new NotImplementedException("StartSession not supported by this plugin");
        }

        public async Task<ActionResult> SessionStartedAsync(SRASession session, string? historyUrl = null, int? historyUrlExpirySeconds = null)
        {
            _logger.LogDebug("SessionStartedAsync called");
            return await Task.FromResult(new ActionResult { IsSuccess = true });
        }

        public async Task<ActionResult> SessionCompleteAsync(SRASession session)
        {
            _logger.LogDebug("SessionCompleteAsync called");
            return await Task.FromResult(new ActionResult { IsSuccess = true });
        }

        #endregion
    }

    /// <summary>
    /// Represents a mapping between a computer name and a Vault secret path
    /// </summary>
    public class EcmMapping
    {
        public string ComputerName { get; set; } = "";
        public string Secret { get; set; } = "";
    }
}
