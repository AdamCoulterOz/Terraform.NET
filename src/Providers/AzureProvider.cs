using TF.Credentials.Azure;

namespace TF.Providers;
public class AzureProvider : Provider
{
	public AzureProvider(Guid subscriptionId, AzureCredential credential) : base(credential)
		=> SubscriptionId = subscriptionId;

	[Terraform("subscription_id", "ARM_SUBSCRIPTION_ID")]
	public Guid SubscriptionId { get; }

	public override string Name => "azurerm";
}
