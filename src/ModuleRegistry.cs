using System.IdentityModel.Tokens.Jwt;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace TF;
public class ModuleRegistry
{
	public ModuleRegistry(Uri endpoint, string jwtToken)
	{
		Endpoint = endpoint;
		Token = new JwtSecurityToken(jwtToken);
	}

	public JwtSecurityToken Token { get; set; }
	public Uri Endpoint { get; set; }

	public async Task DownloadModuleAsync(DirectoryInfo path, ModuleReference moduleReference)
	{
		using var client = new HttpClient();

		var urlBuilder = new UriBuilder(Endpoint)
		{
			Scheme = "https",
			Path = $"v1/modules/{moduleReference.Path}"
		};
		var request = new HttpRequestMessage(HttpMethod.Get, urlBuilder.Uri);
		var authHeader = new AuthenticationHeaderValue("Bearer", Token.RawData);
		request.Headers.Authorization = authHeader;
		var response = await client.SendAsync(request);
		var moduleJson = await response.Content.ReadAsStringAsync();
		var module = JsonSerializer.Deserialize<Module>(moduleJson);
		if(module is null)
			throw new ModuleNotFoundException(moduleReference);
		moduleReference.Version ??= module.LatestVersion;

		urlBuilder.Path += "/download";
		request = new HttpRequestMessage(HttpMethod.Get, urlBuilder.Uri);
		request.Headers.Authorization = authHeader;
		response = await client.SendAsync(request);
		var urlString = response.Headers.GetValues("X-Terraform-Get").First();
		if (response.StatusCode != HttpStatusCode.NoContent || urlString is null)
			throw new ModuleNotFoundException(moduleReference);
		var urlDownload = new Uri(urlString);
		var downloadRequest = new HttpRequestMessage(HttpMethod.Get, urlDownload);
		var download = await client.SendAsync(downloadRequest, HttpCompletionOption.ResponseHeadersRead);
		var filePath = new FileInfo($"{path.FullName}/{moduleReference.FullName}.zip");
		var writeStream = File.OpenWrite(filePath.FullName);
		var readStream = await download.Content.ReadAsStreamAsync();
		await readStream.CopyToAsync(writeStream);
		writeStream.Close();
		readStream.Close();
		ZipFile.ExtractToDirectory(filePath.FullName, filePath.DirectoryName!);
		filePath.Delete();
	}
}
