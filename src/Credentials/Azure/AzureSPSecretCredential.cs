using Azure.Core;
using Azure.Identity;

namespace TF.Credentials.Azure;

/// <summary>
///     Azure AD Application Identity Secret Credential
/// </summary>
public class AzureSPSecretCredential : AzureCredential
{
	/// <param name="subscriptionId">Azure Subscription Id</param>
	/// <param name="tenantId">Azure Organisation (tenant) Id</param>
	/// <param name="clientId">Azure AD Application Client Id</param>
	/// <param name="clientSecret">Azure AD Application Client Secret</param>
	public AzureSPSecretCredential(Guid tenantId, Guid clientId, string clientSecret)
		: base(tenantId, clientId)
	{
		ClientSecret = clientSecret;
	}

	[Terraform("client_secret", "ARM_CLIENT_SECRET")]
	public string ClientSecret { get; set; }

	public override TokenCredential TokenCredential
		=> new ClientSecretCredential(TenantId.ToString(), ClientId.ToString(), ClientSecret.ToString());
}
