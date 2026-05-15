namespace TF.PowerPlatform.Credentials;

public class PowerPlatformOidcCredential(
    Guid tenantId,
    Guid clientId,
    string? oidcToken = null,
    Uri? oidcRequestUri = null,
    string? azureDevOpsServiceConnectionId = null) : Credential
{
    [Terraform("tenant_id", "POWER_PLATFORM_TENANT_ID")]
    public Guid TenantId { get; } = tenantId;

    [Terraform("client_id", "POWER_PLATFORM_CLIENT_ID")]
    public Guid ClientId { get; } = clientId;

    [Terraform("use_oidc", "POWER_PLATFORM_USE_OIDC")]
    public bool UseOidc => true;

    [Terraform("oidc_token", "POWER_PLATFORM_OIDC_TOKEN")]
    public string? OidcToken { get; } = oidcToken;

    [Terraform("oidc_request_url", "POWER_PLATFORM_OIDC_REQUEST_URL")]
    public Uri? OidcRequestUri { get; } = oidcRequestUri;

    [Terraform("azdo_service_connection_id", "POWER_PLATFORM_AZDO_SERVICE_CONNECTION_ID")]
    public string? AzureDevOpsServiceConnectionId { get; } = azureDevOpsServiceConnectionId;
}
