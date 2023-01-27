namespace TF.Credentials.MySql;

public class MySqlCredentialBasic : MySqlCredential
{
	public MySqlCredentialBasic(Uri server, string username, string password) : base(server)
	{
		Username = username;
		Password = password;
	}

	[Terraform("username", "MYSQL_USERNAME")]
	public string Username { get; init; }

	[Terraform("password", "MYSQL_PASSWORD")]
	public string Password { get; init; }

}
