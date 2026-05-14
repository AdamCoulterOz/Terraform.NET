namespace TF.AzureDevOps.Credentials;

public class AzureDevOpsPatCredential(string personalAccessToken) : Credential
{
    [Terraform("personal_access_token", "AZDO_PERSONAL_ACCESS_TOKEN")]
    public string PersonalAccessToken { get; } = personalAccessToken;
}
