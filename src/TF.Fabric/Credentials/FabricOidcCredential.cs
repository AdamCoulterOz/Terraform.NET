namespace TF.Fabric.Credentials;

public class FabricOidcCredential(Guid tenantId, Guid clientId, string? oidcToken = null) : Credential
{
    [Terraform("tenant_id", "FABRIC_TENANT_ID")]
    public Guid TenantId { get; } = tenantId;

    [Terraform("client_id", "FABRIC_CLIENT_ID")]
    public Guid ClientId { get; } = clientId;

    [Terraform("use_oidc", "FABRIC_USE_OIDC")]
    public bool UseOidc => true;

    [Terraform("oidc_token", "FABRIC_OIDC_TOKEN")]
    public string? OidcToken { get; } = oidcToken;
}
