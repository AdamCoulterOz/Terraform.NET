namespace TF;
public class Configuration
{
	public const string ConfigFileEnvVariable = "TF_CLI_CONFIG_FILE";
	public const string ConfigurationFileName = ".terraformrc";

	private Dictionary<string, string> Credentials { get; } = new Dictionary<string, string>();

	public static string FilePath(DirectoryInfo workingFolder)
	    => Path.Combine(workingFolder.FullName, ConfigurationFileName);

	public void AddCredential(Uri hostname, string token) => Credentials.Add(hostname.Host, token);

	public async Task<bool> WriteConfigurationAsync(DirectoryInfo workingFolder)
	{
		var configuration = Credentials.Aggregate("", (current, keyValuePair)
			=> current + $"credentials \"{keyValuePair.Key}\" {{\n\ttoken = \"{keyValuePair.Value}\"\n}}\n\n");

		if (configuration == string.Empty) return false;

		await File.WriteAllTextAsync(FilePath(workingFolder), configuration);
		return true;
	}
}