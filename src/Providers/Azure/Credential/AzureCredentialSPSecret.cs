using Azure.Core;
using Azure.Identity;
using TF.Attributes;

namespace TF.Providers.Azure.Credential;

/// <summary>Azure AD Application Identity Secret Credential</summary>
/// <param name="subscriptionId">Azure Subscription Id</param>
/// <param name="tenantId">Azure Organisation (tenant) Id</param>
/// <param name="clientId">Azure AD Application Client Id</param>
/// <param name="clientSecret">Azure AD Application Client Secret</param>
public class AzureCredentialSPSecret(Guid tenantId, Guid clientId, string clientSecret, Guid? subscriptionId = null) : AzureCredential(tenantId, clientId, subscriptionId)
{
	[CliNamed("ARM_CLIENT_SECRET")]
	public string ClientSecret { get; init; } = clientSecret;

	internal override TokenCredential TokenCredential
		=> new ClientSecretCredential(TenantId.ToString(), ClientId.ToString(), ClientSecret);
}
