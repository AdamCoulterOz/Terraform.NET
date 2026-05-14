namespace TF.AzureDevOps.Providers;

public class AzureDevOpsProvider(Uri organizationUrl, Credential credential) : Provider(credential)
{
    [Terraform("org_service_url", "AZDO_ORG_SERVICE_URL")]
    public Uri OrganizationUrl { get; } = organizationUrl;

    public override string Name => "azuredevops";
}
