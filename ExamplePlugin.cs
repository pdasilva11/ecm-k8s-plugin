using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace MyCompany.Integrations.VaultPlugin
{
    /// <summary>
    /// Service for retrieving secrets from HashiCorp Vault
    /// and providing them for injection into PRA systems
    /// </summary>
    public class VaultService
    {
        private readonly ILogger<VaultService> _logger;
        private readonly VaultConfig _config;
        private readonly HttpClient _httpClient;

        public VaultService(ILogger<VaultService> logger, IOptions<VaultConfig> config)
        {
            _logger = logger;
            _config = config.Value;
            _httpClient = new HttpClient();

            _logger.LogInformation("Vault Service initialized");
            _logger.LogInformation("Vault URL: {VaultUrl}", _config.BaseUrl);
        }

        /// <summary>
        /// Authenticate to Vault and return auth token
        /// </summary>
        public async Task<string> AuthenticateAsync()
        {
            try
            {
                var payload = new { password = _config.Password };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_config.BaseUrl}/v1/auth/userpass/login/{_config.Username}",
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

                _logger.LogInformation("Successfully authenticated to Vault");
                return token ?? throw new Exception("No token in response");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Vault authentication failed");
                throw;
            }
        }

        /// <summary>
        /// Retrieve secret from Vault
        /// </summary>
        public async Task<VaultSecret> GetSecretAsync(string secretPath)
        {
            try
            {
                var token = await AuthenticateAsync();
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("X-Vault-Token", token);

                var response = await _httpClient.GetAsync(
                    $"{_config.BaseUrl}/v1/{_config.SecretsEngine}/data/{secretPath}");

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
                    Path = secretPath,
                    Username = data.TryGetProperty("username", out var userProp)
                        ? userProp.GetString() ?? ""
                        : "",
                    Password = data.TryGetProperty("password", out var passProp)
                        ? passProp.GetString() ?? ""
                        : "",
                    Data = data.EnumerateObject()
                        .ToDictionary(p => p.Name, p => p.Value.GetString() ?? "")
                };

                _logger.LogInformation("Retrieved secret from Vault: {SecretPath}", secretPath);
                return secret;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve secret {SecretPath}", secretPath);
                throw;
            }
        }

        /// <summary>
        /// Perform health check on Vault
        /// </summary>
        public async Task<bool> HealthCheckAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_config.BaseUrl}/v1/sys/health");
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Vault health check passed");
                    return true;
                }
                else
                {
                    _logger.LogWarning("Vault health check failed with status {StatusCode}", response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Vault health check exception");
                return false;
            }
        }
    }

    /// <summary>
    /// Configuration for Vault connection
    /// </summary>
    public class VaultConfig
    {
        public string BaseUrl { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string SecretsEngine { get; set; } = "secret";
    }

    /// <summary>
    /// Represents a secret retrieved from Vault
    /// </summary>
    public class VaultSecret
    {
        public string Path { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public Dictionary<string, string> Data { get; set; } = new();
    }
}
