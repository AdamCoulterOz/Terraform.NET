namespace TF.Credentials.MySql;

public class MySqlBasicCredential : MySqlCredential
{
	private readonly string _username;
	private readonly Uri _server;
	private readonly string _password;

	public MySqlBasicCredential(Uri server, string username, string password)
	{
		_server = server;
		_username = username;
		_password = password;
	}

	[Terraform("username", "MYSQL_USERNAME")]
	public string Username => _username;

	[Terraform("password", "MYSQL_PASSWORD")]
	public string Password => _password;

}
