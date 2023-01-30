namespace TF;
public class Configuration
{
	public const string ConfigFileEnvVariable = "TF_CLI_CONFIG_FILE";
	public const string ConfigurationFileName = ".terraformrc";

	private Dictionary<string, string> Credentials { get; } = new Dictionary<string, string>();

	public static string FilePath(DirectoryInfo workingFolder)
		=> Path.Combine(workingFolder.FullName, ConfigurationFileName);

	/// <summary>
	/// Credential token for a given hostname
	/// </summary>
	/// <param name="hostname"></param>
	/// <param name="token"></param>
	public void AddCredential(Uri hostname, string token) => Credentials.Add(hostname.Host, token);

	public async Task<bool> WriteConfigurationAsync(DirectoryInfo workingFolder)
	{
		var configuration = Credentials.Aggregate(string.Empty, (current, keyValuePair)
			=> current + $$"""
				credentials "{{keyValuePair.Key}}" {
					token = "{{keyValuePair.Value}}"
				}

				""");

		if (configuration == string.Empty) return false;

		await File.WriteAllTextAsync(FilePath(workingFolder), configuration);
		return true;
	}
}