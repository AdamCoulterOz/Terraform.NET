using TF.Attributes;
using TF.Providers.Azure.Credential;

namespace TF.Providers.Azure.Provider;
public class AzureProvider(Guid subscriptionId, AzureCredential credential) : Provider<AzureCredential>(credential)
{
	[CliNamed("ARM_SUBSCRIPTION_ID")]
	public Guid SubscriptionId { get; } = subscriptionId;

    public override string Name => "azurerm";
}
