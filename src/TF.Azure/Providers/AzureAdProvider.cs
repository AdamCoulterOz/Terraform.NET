using TF.Azure.Credentials;

namespace TF.Azure.Providers;

public class AzureAdProvider(AzureCredential credential) : Provider(credential)
{
    public override string Name => "azuread";
}
