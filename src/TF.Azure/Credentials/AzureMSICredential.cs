using Azure.Core;
using Azure.Identity;

namespace TF.Azure.Credentials;

/// <summary>
///     Azure Managed Identity Credential, generates credential from execution context
/// </summary>
/// <remarks>
///     Requires the context to have a managed identity assigned (either System or User assigned)<br />
///     Example contexts: Virtual Machine, Function, Web App
/// </remarks>
/// <param name="tenantId">Azure organisation (tenant) Id</param>
/// <param name="clientId">Specify to use a user assigned managed identity</param>
/// <param name="msiEndpoint">Only required where the MSI endpoint is not standard (e.g. Azure Function App)</param>
public class AzureMSICredential(Guid tenantId, Guid? clientId = null, Uri? msiEndpoint = null) : AzureCredential(tenantId, clientId)
{
    [Terraform("use_msi", "ARM_USE_MSI")] public bool UseMsi => true;

    [Terraform("msi_endpoint", "ARM_MSI_ENDPOINT")]
    public Uri? MsiEndpoint { get; } = msiEndpoint;

    public override TokenCredential TokenCredential
		=> ClientId is Guid clientId
			? new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(clientId.ToString()))
			: new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
}
