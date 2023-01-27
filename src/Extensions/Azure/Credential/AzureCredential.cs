using Azure.Core;

namespace TF.Credentials.Azure;
public abstract class AzureCredential : Credential
{
	public AzureCredential(Guid tenantId, Guid? clientId = null, Guid? subscriptionId = null)
	{
		TenantId = tenantId;
		ClientId = clientId;
		SubscriptionId = subscriptionId;
	}

	[Terraform("tenant_id", "ARM_TENANT_ID")]
	public required Guid TenantId { get; init; }

	[Terraform("client_id", "ARM_CLIENT_ID")]
	public Guid? ClientId { get; init; }

	[Terraform("subscription_id", "ARM_SUBSCRIPTION_ID")]
	public Guid? SubscriptionId { get; init; }

	public override string ToString() => $"TenantId: {TenantId} | ClientId: {ClientId}";

	public abstract TokenCredential TokenCredential { get; }
}