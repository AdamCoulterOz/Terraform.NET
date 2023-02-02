using TF.Attributes;

namespace TF.Providers.MySql.Credential;

public abstract class MySqlCredential : TF.Credential
{
	public MySqlCredential(Uri server) => Server = server;

	[CliNamed("MYSQL_SERVER")]
	public Uri Server { get; init; }
}
