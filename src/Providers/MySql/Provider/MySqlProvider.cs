using TF.Attributes;
using TF.Providers.MySql.Credential;

namespace TF.Providers.MySql.Provider;
public class MySqlProvider(Uri server, MySqlCredential credential) : Provider<MySqlCredential>(credential)
{
	[CliNamed("MYSQL_ENDPOINT")]
	protected string Endpoint => $"{server.Host}:{server.Port}";

	[CliNamed("ALL_PROXY")]
	public Uri? Proxy { get; set; }

	[CliNamed("MYSQL_TLS_CONFIG")]
	public bool TLS { get; set; } = false;

	public override string Name =>  "mysql";
}
