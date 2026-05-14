namespace TF.PowerPlatform.Credentials;

public class PowerPlatformServicePrincipalCredential(Guid tenantId, Guid clientId, string clientSecret) : Credential
{
    [Terraform("tenant_id", "POWER_PLATFORM_TENANT_ID")]
    public Guid TenantId { get; } = tenantId;

    [Terraform("client_id", "POWER_PLATFORM_CLIENT_ID")]
    public Guid ClientId { get; } = clientId;

    [Terraform("client_secret", "POWER_PLATFORM_CLIENT_SECRET")]
    public string ClientSecret { get; } = clientSecret;
}
