using Azure.Core;
using Azure.Identity;

namespace TF.Azure.Credentials;

/// <summary>
///     Azure AD Application Identity Secret Credential
/// </summary>
/// <param name="subscriptionId">Azure Subscription Id</param>
/// <param name="tenantId">Azure Organisation (tenant) Id</param>
/// <param name="clientId">Azure AD Application Client Id</param>
/// <param name="clientSecret">Azure AD Application Client Secret</param>
public class AzureSPSecretCredential(Guid tenantId, Guid clientId, string clientSecret) : AzureCredential(tenantId, clientId)
{
    [Terraform("client_secret", "ARM_CLIENT_SECRET")]
    public string ClientSecret { get; set; } = clientSecret;

    public override TokenCredential TokenCredential
		=> new ClientSecretCredential(TenantId.ToString(), ClientId.ToString(), ClientSecret.ToString());
}
