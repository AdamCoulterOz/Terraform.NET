using Azure.Core;
using Azure.Identity;
using TF.Attributes;

namespace TF.Providers.Azure.Credential;

/// <summary>
///     Azure Managed Identity Credential, generates credential from execution context
/// </summary>
/// <remarks>
///     Requires the context to have a managed identity assigned (either System or User assigned)<br />
///     Example contexts: Virtual Machine, Function, Web App
/// </remarks>
/// <param name="tenantId">Azure organisation (tenant) Id</param>
/// <param name="clientId">Override the system assigned identity (if applicable) with a user assigned managed identity</param>
/// <param name="msiEndpoint">Only required where the MSI endpoint is not standard (e.g. Azure Function App)</param>
public class AzureCredentialMSI(Guid tenantId, Guid? clientId = null, Uri? msiEndpoint = null) : AzureCredential(tenantId, clientId)
{
	[CliNamed("ARM_USE_MSI")]
	protected static bool UseMsi => true;

	[CliNamed("ARM_MSI_ENDPOINT")]
	public Uri? MsiEndpoint { get; init; } = msiEndpoint;

	internal override TokenCredential TokenCredential
		=> new ManagedIdentityCredential(ClientId?.ToString());
}
