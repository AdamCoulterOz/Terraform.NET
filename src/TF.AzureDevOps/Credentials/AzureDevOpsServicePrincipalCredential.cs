namespace TF.AzureDevOps.Credentials;

public class AzureDevOpsServicePrincipalCredential(Guid tenantId, Guid clientId, string clientSecret) : Credential, IEntraCredential
{
    [Terraform("tenant_id", "ARM_TENANT_ID")]
    public Guid TenantId { get; } = tenantId;

    [Terraform("client_id", "ARM_CLIENT_ID")]
    public Guid ClientId { get; } = clientId;

    [Terraform("client_secret", "ARM_CLIENT_SECRET")]
    public string ClientSecret { get; } = clientSecret;
}
