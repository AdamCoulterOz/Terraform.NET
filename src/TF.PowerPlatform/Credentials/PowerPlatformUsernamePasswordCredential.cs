namespace TF.PowerPlatform.Credentials;

// Resource Owner Password Credentials (ROPC) for a contained delegated escape-hatch account (e.g.
// SVC.ELMO) on a dedicated provider ALIAS, where a Dataverse operation refuses app-only tokens. The
// client id must be a public client that permits ROPC (e.g. the Azure CLI public client).
//
// Deliberately HCL-block-only: the [Terraform] attributes carry NO env-var name, so this credential
// contributes nothing to the process-wide provider env (ProviderCollection.CombinedProviderConfigs).
// That isolation is essential — if username/password leaked into the global env they would hijack the
// DEFAULT service-principal provider (the provider's auth switch checks username/password before
// client_secret). The alias's config lives solely in its own provider block. It still inherits the
// (harmless) client_secret from the global env, but the auth switch selects username/password first,
// so the alias authenticates ROPC as the escape-hatch account.
public class PowerPlatformUsernamePasswordCredential(Guid tenantId, Guid clientId, string username, string password) : Credential, IEntraCredential
{
    [Terraform("tenant_id")]
    public Guid TenantId { get; } = tenantId;

    [Terraform("client_id")]
    public Guid ClientId { get; } = clientId;

    [Terraform("username")]
    public string Username { get; } = username;

    [Terraform("password")]
    public string Password { get; } = password;
}
