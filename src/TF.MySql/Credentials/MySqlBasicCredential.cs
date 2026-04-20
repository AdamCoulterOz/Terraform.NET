namespace TF.MySql.Credentials;

public class MySqlBasicCredential(Uri server, string username, string password) : MySqlCredential
{
	private readonly string _username = username;
	private readonly Uri _server = server;
	private readonly string _password = password;

    [Terraform("username", "MYSQL_USERNAME")]
	public string Username => _username;

	[Terraform("password", "MYSQL_PASSWORD")]
	public string Password => _password;
}
