using System.ComponentModel;
using Azure.Core;

namespace TF.Azure.Credentials;

public abstract class AzureCredential(Guid tenantId, Guid? clientId = null) : Credential
{
    [Terraform("tenant_id", "ARM_TENANT_ID")]
    public Guid TenantId { get; } = tenantId;

    [Terraform("client_id", "ARM_CLIENT_ID")]
    public Guid? ClientId { get; } = clientId;

    public override string ToString() => $"TenantId: {TenantId} | ClientId: {ClientId}";

    public abstract TokenCredential TokenCredential { get; }

	public static AzureCredential GetCredential(Guid tenantId, Guid? clientId = null, string? clientSecret = null)
	{
		clientId = GetValueOrDefault(clientId, "AZURE_CLIENT_ID");
		clientSecret = GetValueOrDefault(clientSecret, "AZURE_CLIENT_SECRET");
		if (clientSecret is not null && clientId is null)
			throw new Exception("If a client secret is set, so must a client id");

		return clientSecret is null
			? new AzureMSICredential(tenantId, clientId)
			: new AzureSPSecretCredential(tenantId, clientId!.Value, clientSecret);
	}

	private static T? GetValueOrDefault<T>(T value, string envVarName)
	{
		if (value is not null) return value;
		var envValue = Environment.GetEnvironmentVariable(envVarName);
		if (envValue is null) return default;
		return (T?)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(envValue);
	}
}
