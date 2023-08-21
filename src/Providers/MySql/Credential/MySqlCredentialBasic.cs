using TF.Attributes;

namespace TF.Providers.MySql.Credential;

public class MySqlCredentialBasic(Uri server, string username, string password) : MySqlCredential(server)
{
	[CliNamed("MYSQL_USERNAME")]
	public string Username { get; init; } = username;

	[CliNamed("MYSQL_PASSWORD")]
	public string Password { get; init; } = password;
}
