using Azure.Core;

namespace TF.Azure.Credentials;

public class AzureOidcCredential(
    Guid tenantId,
    Guid clientId,
    string? oidcToken = null,
    string? oidcRequestToken = null,
    Uri? oidcRequestUrl = null,
    string? azureDevOpsServiceConnectionId = null) : AzureCredential(tenantId, clientId), IEntraOidcCredential
{
    [Terraform("use_oidc", "ARM_USE_OIDC")]
    public bool UseOidc => true;

    [Terraform("oidc_token", "ARM_OIDC_TOKEN")]
    public string? OidcToken { get; } = oidcToken;

    [Terraform("oidc_request_token", "ARM_OIDC_REQUEST_TOKEN")]
    public string? OidcRequestToken { get; } = oidcRequestToken;

    [Terraform("oidc_request_url", "ARM_OIDC_REQUEST_URL")]
    public Uri? OidcRequestUrl { get; } = oidcRequestUrl;

    [Terraform("ado_pipeline_service_connection_id", "ARM_ADO_PIPELINE_SERVICE_CONNECTION_ID")]
    public string? AzureDevOpsServiceConnectionId { get; } = azureDevOpsServiceConnectionId;

    public override TokenCredential TokenCredential =>
        throw new NotSupportedException("Azure OIDC credentials are intended for Terraform provider/backend environment configuration and do not expose an Azure SDK TokenCredential.");

    Guid IEntraCredential.ClientId => ClientId ?? throw new InvalidOperationException("Azure OIDC credentials always require a client id.");
}
