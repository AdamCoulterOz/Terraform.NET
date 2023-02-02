using TF.Attributes;
using TF.Providers.MySql.Credential;

namespace TF.Providers.MySql.Provider;
public class MySqlProvider : TF.Provider
{
	private readonly Uri _server;

	public MySqlProvider(Uri server, MySqlCredential credential) : base(credential)
		=> _server = server;

	[CliNamed("MYSQL_ENDPOINT")]
	protected string Endpoint => $"{_server.Host}:{_server.Port}";

	[CliNamed("ALL_PROXY")]
	public Uri? Proxy { get; set; }

	[CliNamed("MYSQL_TLS_CONFIG")]
	public bool TLS { get; set; } = false;

	protected internal override string Name => "mysql";
}
