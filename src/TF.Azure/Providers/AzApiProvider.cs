using TF.Azure.Credentials;

namespace TF.Azure.Providers;

public class AzApiProvider(AzureCredential credential) : Provider(credential)
{
    public override string Name => "azapi";
    public override string Source => "azure/azapi";

    protected override Dictionary<string, TFValue> GetTerraformConfig()
    {
        var config = base.GetTerraformConfig();
        config.Remove("ado_pipeline_service_connection_id");
        return config;
    }
}
