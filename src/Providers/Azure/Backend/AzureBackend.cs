using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using TF.Attributes;
using TF.Providers.Azure.Credential;

namespace TF.Providers.Azure.Backend;

/// <summary>
///     Stores the state as a Blob with the given Key within the Blob Container within the Blob Storage Account.
///     This backend also supports state locking and consistency checking via native capabilities of Azure Blob Storage.
/// </summary>
public class AzureBackend : Backend<AzureCredential>
{
	protected override string Name => "azurerm";

	public AzureBackend(AzureCredential credential) : base(credential)
	{
		CreateContainerIfNotExists();
	}

	private void CreateContainerIfNotExists()
	{
		var accountUri = new Uri($"https://{StorageAccountName.ToLowerInvariant()}.blob.core.windows.net/{ContainerName.ToLowerInvariant()}");
		var container = new BlobContainerClient(accountUri, Credential.TokenCredential);
		container.CreateIfNotExists(PublicAccessType.None);
	}

	[CliNamed("tenant_id")]
	public required Guid TenantId { get; init; }

	[CliNamed("subscription_id")]
	public required Guid SubscriptionId { get; init; }

	[CliNamed("resource_group_name")]
	public required string ResourceGroupName { get; init; }

	[CliNamed("storage_account_name")]
	public required string StorageAccountName { get; init; }

	[CliNamed("container_name")]
	public required string ContainerName { get; init; }

	[CliNamed("key")]
	public required string BlobName { get; init; }
}
