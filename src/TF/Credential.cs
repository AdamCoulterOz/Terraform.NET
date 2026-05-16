namespace TF;
public abstract class Credential { }

public interface IEntraCredential
{
    Guid TenantId { get; }
    Guid ClientId { get; }
}

public interface IEntraOidcCredential : IEntraCredential
{
    bool UseOidc { get; }
    string? OidcToken { get; }
    string? OidcRequestToken { get; }
    Uri? OidcRequestUrl { get; }
    string? AzureDevOpsServiceConnectionId { get; }
}
