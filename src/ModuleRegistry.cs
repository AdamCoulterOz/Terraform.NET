using System.IdentityModel.Tokens.Jwt;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;

namespace TF;
public class ModuleRegistry
{
	public ModuleRegistry(Uri endpoint) : this(endpoint, endpoint.Host) { }

	public ModuleRegistry(Uri endpoint, string oAuthTarget)
	{
		Endpoint = endpoint;
		Token = GetOAuthToken(oAuthTarget);
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
		var version = module!.LatestVersion;

		urlBuilder = new UriBuilder(Endpoint)
		{
			Scheme = "https",
			Path = $"v1/modules/{moduleReference.Path}/{version}/download"
		};
		request = new HttpRequestMessage(HttpMethod.Get, urlBuilder.Uri);
		request.Headers.Authorization = authHeader;
		response = await client.SendAsync(request);
		var urlString = response.Headers.GetValues("X-Terraform-Get").First();
		if (response.StatusCode != HttpStatusCode.NoContent || urlString is null)
			throw new Exception(
				$"Registry didnt return valid module response. Is the module reference correct? {moduleReference}");
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

	public static JwtSecurityToken GetOAuthToken(string targetAudience)
	{
		var options = new DefaultAzureCredentialOptions
		{ ExcludeSharedTokenCacheCredential = true };
		var credential = new DefaultAzureCredential(options);
		var tokenContext = new TokenRequestContext(new[] { $"{targetAudience}/.default" });
		var accessToken = credential.GetToken(tokenContext);
		return new JwtSecurityToken(accessToken.Token);
	}
}
