namespace MyCompany.Integrations.ExamplePlugin
{
    /// <summary>
    /// Represents a simple POCO that defines the config values necessary for the plugin to operate
    /// These are simply examples of the types of items which might be required for your implementation
    /// </summary>
    public sealed record ExamplePluginConfig
    {
        public required string ApiBaseUrl { get; init; }
        public required string OAuthClientId { get; init; }
        public required string OAuthClientSecret { get; init; }
        public bool EnableSpecialCase { get; init; } = false;
    }
}
