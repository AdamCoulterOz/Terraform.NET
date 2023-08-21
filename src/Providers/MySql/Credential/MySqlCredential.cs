using TF.Attributes;

namespace TF.Providers.MySql.Credential;

public abstract class MySqlCredential(Uri server) : TF.Credential
{
	[CliNamed("MYSQL_SERVER")]
	public Uri Server { get; init; } = server;
}
