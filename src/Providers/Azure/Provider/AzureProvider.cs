using TF.Attributes;
using TF.Providers.Azure.Credential;

namespace TF.Providers.Azure.Provider;
public class AzureProvider : TF.Provider
{
	public AzureProvider(Guid subscriptionId, AzureCredential credential) : base(credential)
		=> SubscriptionId = subscriptionId;

	[CliNamed("ARM_SUBSCRIPTION_ID")]
	public Guid SubscriptionId { get; }

	public override string Name => "azurerm";
}
