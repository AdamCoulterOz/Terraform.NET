namespace TF.Extensions;
public static class IOExtensions
{
	public static void WriteAllText(this FileInfo file, string content, bool overwrite = true)
	{
		if (file.Exists && !overwrite)
			throw new Exception($"File '{file.FullName}' already exists");
		File.WriteAllText(file.FullName, content);
	}
}
