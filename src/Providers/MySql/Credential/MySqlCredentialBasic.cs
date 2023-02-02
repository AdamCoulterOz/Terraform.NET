using TF.Attributes;

namespace TF.Providers.MySql.Credential;

public class MySqlCredentialBasic : MySqlCredential
{
	public MySqlCredentialBasic(Uri server, string username, string password) : base(server)
	{
		Username = username;
		Password = password;
	}

	[CliNamed("MYSQL_USERNAME")]
	public string Username { get; init; }

	[CliNamed("MYSQL_PASSWORD")]
	public string Password { get; init; }

}
