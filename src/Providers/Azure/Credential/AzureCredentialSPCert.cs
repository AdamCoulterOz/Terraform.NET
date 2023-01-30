using System.Security;
using System.Security.Cryptography.X509Certificates;
using Azure.Core;
using Azure.Identity;

namespace TF.Providers.Azure.Credential;

/// <summary>
///     Azure AD Application Identity Certificate Credential
///     <see
///         href="https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/guides/service_principal_client_certificate">
///         Information
///         on how to setup a certificate credential
///     </see>
/// </summary>
public class AzureCredentialSPCert : AzureCredential
{
	/// <param name="tenantId">Azure Organisation (tenant) Id</param>
	/// <param name="clientId">Azure AD Application Client Id</param>
	/// <param name="certificatePath">File path to X509 certificate</param>
	/// <param name="certificatePassword">Certificate password</param>
	public AzureCredentialSPCert(Guid tenantId, Guid clientId, string certificatePath,
		SecureString certificatePassword, Guid? subscriptionId = null)
		: base(tenantId, clientId, subscriptionId)
	{
		CertificatePath = new FileInfo(certificatePath);
		if (!CertificatePath.Exists)
			throw new Exception($"Certificate at path: '{CertificatePath}' cannot be found");
		CertificatePassword = certificatePassword;
		if (!Certificate.Verify())
			throw new Exception($"Certificate cannot be verified");
	}

	private X509Certificate2 Certificate
		=> new(CertificatePath.FullName, CertificatePassword);

	[Terraform("client_certificate_path", "ARM_CLIENT_CERTIFICATE_PATH")]
	public FileInfo CertificatePath { get; }

	[Terraform("client_certificate_password", "ARM_CLIENT_CERTIFICATE_PASSWORD")]
	public SecureString CertificatePassword { get; }

	public override TokenCredential TokenCredential
		=> new ClientCertificateCredential(TenantId.ToString(), ClientId.ToString(), Certificate);
}
