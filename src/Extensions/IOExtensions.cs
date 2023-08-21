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

	public static async Task CopyToAsync(this DirectoryInfo source, DirectoryInfo destination, bool recursive = true)
	{
		await Task.Run(() => source.CopyTo(destination, recursive));
	}

	public static void CopyTo(this DirectoryInfo source, DirectoryInfo destination, bool recursive = true)
	{
		if (!source.Exists)
			throw new ArgumentException($"Source directory '{source.FullName}' does not exist", nameof(source));
		if (!destination.Exists)
			destination.Create();
		foreach (var file in source.GetFiles())
			file.CopyTo(Path.Combine(destination.FullName, file.Name));
		if (recursive)
			foreach (var directory in source.GetDirectories())
				directory.CopyTo(new DirectoryInfo(Path.Combine(destination.FullName, directory.Name)));
	}
}
