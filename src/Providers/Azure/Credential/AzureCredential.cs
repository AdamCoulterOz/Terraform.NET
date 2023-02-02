using Azure.Core;
using TF.Attributes;

namespace TF.Providers.Azure.Credential;
public abstract class AzureCredential : TF.Credential
{
	public AzureCredential(Guid tenantId, Guid? clientId = null, Guid? subscriptionId = null)
	{
		TenantId = tenantId;
		ClientId = clientId;
		SubscriptionId = subscriptionId;
	}

	[CliNamed("ARM_TENANT_ID")]
	public required Guid TenantId { get; init; }

	[CliNamed("ARM_CLIENT_ID")]
	public Guid? ClientId { get; init; }

	[CliNamed("ARM_SUBSCRIPTION_ID")]
	public Guid? SubscriptionId { get; init; }

	public abstract TokenCredential TokenCredential { get; }
}