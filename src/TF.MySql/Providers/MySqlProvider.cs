using TF.MySql.Credentials;

namespace TF.MySql.Providers;
public class MySqlProvider(Uri server, MySqlCredential credential) : Provider(credential)
{
	private readonly Uri _server = server;

    [Terraform("endpoint", "MYSQL_ENDPOINT")]
	public string Endpoint => $"{_server.Host}:{_server.Port}";

	[Terraform("proxy", "ALL_PROXY")]
	public Uri? Proxy { get; set; }

	[Terraform("tls", "MYSQL_TLS_CONFIG", Lower = true)]
	public bool TLS { get; set; } = false;

	public override string Name => "mysql";
}
