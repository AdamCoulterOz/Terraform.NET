using TF;
using TF.Credentials.MySql;

namespace TF.Extensions.MySql.Provider;
public class MySqlProvider : TF.Provider
{
	private readonly Uri _server;

	public MySqlProvider(Uri server, MySqlCredential credential) : base(credential)
		=> _server = server;

	[Terraform("endpoint", "MYSQL_ENDPOINT")]
	public string Endpoint => $"{_server.Host}:{_server.Port}";

	[Terraform("proxy", "ALL_PROXY")]
	public Uri? Proxy { get; set; }

	[Terraform("tls", "MYSQL_TLS_CONFIG", Lower = true)]
	public bool TLS { get; set; } = false;

	public override string Name => "mysql";
}
