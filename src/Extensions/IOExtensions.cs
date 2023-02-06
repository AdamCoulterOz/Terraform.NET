namespace TF.Extensions;
public static class IOExtensions
{
	public static void WriteAllText(this FileInfo file, string content, bool overwrite = false)
	{
		if (file.Exists && !overwrite)
			throw new ArgumentException($"File '{file.FullName}' already exists", nameof(file));
		File.WriteAllText(file.FullName, content);
	}

	public static DirectoryInfo AsDirectory(this string path)
		=> new(path);

	public static DirectoryInfo GetNewTempDirectory()
	{
		var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
		Directory.CreateDirectory(path);
		return new(path);
	}
}
