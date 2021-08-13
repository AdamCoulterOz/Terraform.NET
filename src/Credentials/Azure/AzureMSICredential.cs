using Azure.Core;
using Azure.Identity;

namespace TF.Credentials.Azure;

/// <summary>
///     Azure Managed Identity Credential, generates credential from execution context
/// </summary>
/// <remarks>
///     Requires the context to have a managed identity assigned (either System or User assigned)<br />
///     Example contexts: Virtual Machine, Function, Web App
/// </remarks>
public class AzureMSICredential : AzureCredential
{
	/// <param name="tenantId">Azure organisation (tenant) Id</param>
	/// <param name="clientId">Specify to use a user assigned managed identity</param>
	/// <param name="msiEndpoint">Only required where the MSI endpoint is not standard (e.g. Azure Function App)</param>
	public AzureMSICredential(Guid tenantId, Guid? clientId = null, Uri? msiEndpoint = null)
		: base(tenantId, clientId)
	{
		MsiEndpoint = msiEndpoint;
	}

	[Terraform("use_msi", "ARM_USE_MSI")] public bool UseMsi => true;

	[Terraform("msi_endpoint", "ARM_MSI_ENDPOINT")]
	public Uri? MsiEndpoint { get; }

	public override TokenCredential TokenCredential
		=> new ManagedIdentityCredential(ClientId?.ToString());
}
