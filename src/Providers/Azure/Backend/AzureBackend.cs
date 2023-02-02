using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using TF.Attributes;
using TF.Providers.Azure.Credential;

namespace TF.Providers.Azure.Backend;

/// <summary>
///     Stores the state as a Blob with the given Key within the Blob Container within the Blob Storage Account.
///     This backend also supports state locking and consistency checking via native capabilities of Azure Blob Storage.
/// </summary>
/// <remarks>
///     Resource Path: Subscription/ResourceGroup/StorageAccount/Container/Blob
/// </remarks>
public class AzureBackend : Backend<AzureCredential>
{
	protected override string Name => "azurerm";

	public AzureBackend(AzureCredential credential, Guid tenantId, Guid subscriptionId,
		string resourceGroupName, string storageAccountName, string containerName, string blobName) : base(credential)
	{
		Credential = credential;
		TenantId = tenantId;
		SubscriptionId = subscriptionId;
		ResourceGroupName = resourceGroupName;
		StorageAccountName = storageAccountName;
		ContainerName = containerName;
		BlobName = blobName;
		ValidateCreateContainer();
	}

	/// <summary>
	///     Construct from storage account ARM resource id
	/// </summary>
	/// <remarks>
	///     This simplifies the need to parse the resource id components separately
	///     e.g. subscriptionId, resourceGroupName and storageAccountName
	/// </remarks>
	/// <param name="credential">
	///     Azure credential to use to connect to the blob storage backend, needs to have the blob storage owner permission
	/// </param>
	/// <param name="storageAccountResourceId">
	///     The format output from the azurerm_storage_account.xxx.id
	///     <see langword="Microsoft.Storage/storageAccounts" />
	/// </param>
	/// <param name="containerName">The container in the storage account</param>
	/// <param name="blobName"></param>
	public AzureBackend(AzureCredential credential, Guid tenantId, string storageAccountResourceId,
		string containerName, string blobName) : base(credential)
	{
		TenantId = tenantId;
		const string storageAccountResourceType = "Microsoft.Storage/storageAccounts";
		var storageAccount = ResourceId.FromString(storageAccountResourceId);
		if (storageAccount.FullResourceType != storageAccountResourceType)
			throw new Exception($"Provided resourceId isn't of type '{storageAccountResourceType}'");
		SubscriptionId = Guid.Parse(storageAccount.SubscriptionId);
		ResourceGroupName = storageAccount.ResourceGroupName;
		StorageAccountName = storageAccount.Name;
		ContainerName = containerName;
		BlobName = blobName;
		ValidateCreateContainer();
	}

	private void ValidateCreateContainer()
	{
		var accountUri = new Uri($"https://{StorageAccountName.ToLowerInvariant()}.blob.core.windows.net/{ContainerName.ToLowerInvariant()}");
		var container = new BlobContainerClient(accountUri, Credential!.TokenCredential);
		container.CreateIfNotExists(PublicAccessType.None);
	}

	[CliNamed("resource_group_name")]
	public required string ResourceGroupName { get; init; }

	[CliNamed("storage_account_name")]
	public required string StorageAccountName { get; init; }

	[CliNamed("subscription_id")]
	public required Guid SubscriptionId { get; init; }

	[CliNamed("tenant_id")]
	public required Guid TenantId { get; init; }

	[CliNamed("container_name")]
	public required string ContainerName { get; init; }

	[CliNamed("key")]
	public required string BlobName { get; init; }
}
