namespace MyCompany.Integrations.VaultPlugin
{
    /// <summary>
    /// Configuration for the Vault ECM Plugin
    /// </summary>
    public sealed record VaultPluginConfig
    {
        /// <summary>
        /// HashiCorp Vault base URL
        /// </summary>
        public required string VaultBaseUrl { get; init; }

        /// <summary>
        /// Vault username for authentication
        /// </summary>
        public required string VaultUsername { get; init; }

        /// <summary>
        /// Vault password for authentication
        /// </summary>
        public required string VaultPassword { get; init; }

        /// <summary>
        /// Vault secrets engine path (default: "secret")
        /// </summary>
        public string VaultSecretsEngine { get; init; } = "secret";
    }
}
