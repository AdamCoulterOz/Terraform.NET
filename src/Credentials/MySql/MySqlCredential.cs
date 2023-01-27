namespace TF.Credentials.MySql;

public abstract class MySqlCredential : Credential
{
	public MySqlCredential(Uri server) => Server = server;

	[Terraform("server", "MYSQL_SERVER")]
	public Uri Server { get; init; }
}
