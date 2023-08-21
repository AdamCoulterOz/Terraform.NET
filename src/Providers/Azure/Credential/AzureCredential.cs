using Azure.Core;
using TF.Attributes;

namespace TF.Providers.Azure.Credential;
public abstract class AzureCredential(Guid tenantId, Guid? clientId = null, Guid? subscriptionId = null) : TF.Credential
{
	[CliNamed("ARM_TENANT_ID")]
	public required Guid TenantId { get; init; } = tenantId;

	[CliNamed("ARM_CLIENT_ID")]
	public Guid? ClientId { get; init; } = clientId;

	[CliNamed("ARM_SUBSCRIPTION_ID")]
	public Guid? SubscriptionId { get; init; } = subscriptionId;

	internal abstract TokenCredential TokenCredential { get; }
}