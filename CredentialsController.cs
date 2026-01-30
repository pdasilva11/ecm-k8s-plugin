using Microsoft.AspNetCore.Mvc;
using MyCompany.Integrations.VaultPlugin;

namespace MyCompany.Integrations.VaultPlugin.Controllers
{
    /// <summary>
    /// API controller for PRA to retrieve and inject credentials from Vault
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CredentialsController : ControllerBase
    {
        private readonly VaultService _vaultService;
        private readonly ILogger<CredentialsController> _logger;

        public CredentialsController(VaultService vaultService, ILogger<CredentialsController> logger)
        {
            _vaultService = vaultService;
            _logger = logger;
        }

        /// <summary>
        /// Get credential for injection by secret path
        /// </summary>
        /// <param name="secretPath">Path to secret in Vault (e.g., "db/prod/admin")</param>
        /// <returns>Credential with username and password</returns>
        [HttpGet("secret/{*secretPath}")]
        public async Task<ActionResult<VaultSecret>> GetSecret(string secretPath)
        {
            try
            {
                _logger.LogInformation("Credential request for secret: {SecretPath}", secretPath);
                var secret = await _vaultService.GetSecretAsync(secretPath);
                return Ok(secret);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve secret: {SecretPath}", secretPath);
                return StatusCode(500, new { error = "Failed to retrieve secret", details = ex.Message });
            }
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        [HttpGet("health")]
        public async Task<ActionResult> Health()
        {
            try
            {
                var isHealthy = await _vaultService.HealthCheckAsync();
                if (isHealthy)
                {
                    return Ok(new { status = "healthy", message = "Vault connection is operational" });
                }
                else
                {
                    return StatusCode(503, new { status = "unhealthy", message = "Vault is not responding" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return StatusCode(503, new { status = "unhealthy", error = ex.Message });
            }
        }

        /// <summary>
        /// Root endpoint for API info
        /// </summary>
        [HttpGet("")]
        public ActionResult<object> Info()
        {
            return Ok(new
            {
                service = "Vault Credential Injection Service",
                version = "1.0",
                endpoints = new
                {
                    getSecret = "/api/credentials/secret/{secretPath}",
                    health = "/api/credentials/health"
                }
            });
        }
    }
}
