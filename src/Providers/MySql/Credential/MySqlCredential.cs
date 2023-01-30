namespace TF.Providers.MySql.Credential;

public abstract class MySqlCredential : TF.Credential
{
	public MySqlCredential(Uri server) => Server = server;

	[Terraform("server", "MYSQL_SERVER")]
	public Uri Server { get; init; }
}
