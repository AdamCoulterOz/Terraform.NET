using TF.Azure.Credentials;

namespace TF.Azure.Providers;

public class AzureProvider(Guid subscriptionId, AzureCredential credential) : Provider(credential)
{
    [Terraform("subscription_id", "ARM_SUBSCRIPTION_ID")]
    public Guid SubscriptionId { get; } = subscriptionId;

    [Terraform("features")]
    public Dictionary<string, object?> Features { get; } = [];

    public override string Name => "azurerm";
}
