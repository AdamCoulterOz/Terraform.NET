using System.IdentityModel.Tokens.Jwt;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace TF;
public class ModuleRegistry
{
	public JwtSecurityToken Token { get; set; }
	public Uri Endpoint { get; set; }
	public ModuleRegistry(Uri endpoint, string jwtToken)
	{
		Endpoint = endpoint;
		Token = new JwtSecurityToken(jwtToken);
	}

	public async Task<Module> GetModuleAsync(ModuleReference moduleReference)
	{
		var response = await httpRequest(HttpMethod.Get, $"v1/modules/{moduleReference.Path}");
		var moduleJson = await response.Content.ReadAsStringAsync();
		var module = JsonSerializer.Deserialize<Module>(moduleJson);
		if (module is null)
			throw new ModuleNotFoundException(moduleReference);
		return module;
	}

	public async Task DownloadModuleAsync(DirectoryInfo path, ModuleReference moduleReference)
	{
		moduleReference.Version ??= (await GetModuleAsync(moduleReference)).LatestVersion;

		var response = await httpRequest(HttpMethod.Get, $"v1/modules/{moduleReference.Path}/download");
		var urlString = response.Headers.GetValues("X-Terraform-Get").First();
		if (response.StatusCode != HttpStatusCode.NoContent || urlString is null)
			throw new ModuleNotFoundException(moduleReference);

		var urlDownload = new Uri(urlString);
		var download = await httpRequest(HttpMethod.Get, urlDownload, true);
		var filePath = new FileInfo($"{path.FullName}/{moduleReference.FullName}.zip");
		var writeStream = File.OpenWrite(filePath.FullName);
		var readStream = await download.Content.ReadAsStreamAsync();
		await readStream.CopyToAsync(writeStream);
		writeStream.Close();
		readStream.Close();
		ZipFile.ExtractToDirectory(filePath.FullName, filePath.DirectoryName!);
		filePath.Delete();
	}

	private AuthenticationHeaderValue _authHeader => new AuthenticationHeaderValue("Bearer", Token.RawData);
	private async Task<HttpResponseMessage> httpRequest(HttpMethod method, string path, bool justHeaders = false)
		=> await httpRequest(method, new UriBuilder(Endpoint) { Path = path }.Uri, justHeaders);
	private async Task<HttpResponseMessage> httpRequest(HttpMethod method, Uri uri, bool justHeaders = false)
	{
		using var client = new HttpClient();
		var request = new HttpRequestMessage(method, uri);
		request.Headers.Authorization = _authHeader;
		var completionOption = justHeaders ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead;
		var response = await client.SendAsync(request, completionOption);
		return response;
	}
}
