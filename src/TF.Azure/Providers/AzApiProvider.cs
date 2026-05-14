using TF.Azure.Credentials;

namespace TF.Azure.Providers;

public class AzApiProvider(AzureCredential credential) : Provider(credential)
{
    public override string Name => "azapi";
}
