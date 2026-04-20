using System.IdentityModel.Tokens.Jwt;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace TF;
public class ModuleRegistry
{
	private readonly Func<Task<JwtSecurityToken>> _getToken;
	private JwtSecurityToken? _token;
	private async Task<JwtSecurityToken> GetTokenAsync() => _token ??= await _getToken();
	public Uri Endpoint { get; init; }

	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

	public ModuleRegistry(Uri endpoint, JwtSecurityToken jwtToken)
		=> (Endpoint, _token, _getToken) = (endpoint, jwtToken, () => Task.FromResult(jwtToken));

	public ModuleRegistry(Uri endpoint, Func<Task<JwtSecurityToken>> getJwtToken)
		=> (Endpoint, _getToken) = (endpoint, getJwtToken);

	public async Task<Module> GetModuleAsync(ModuleReference moduleReference)
	{
		var response = await HttpRequest(HttpMethod.Get, $"v1/modules/{moduleReference.Path}");
		var moduleJson = await response.Content.ReadAsStringAsync();
        var module = JsonSerializer.Deserialize<Module>(moduleJson, JsonOptions)
			?? throw new ModuleNotFoundException(moduleReference);
        return module;
	}

	public async Task DownloadModuleAsync(DirectoryInfo path, ModuleReference moduleReference)
	{
		moduleReference.Version ??= (await GetModuleAsync(moduleReference)).LatestVersion;

		var response = await HttpRequest(HttpMethod.Get, $"v1/modules/{moduleReference.Path}/download");
		if (response.StatusCode != HttpStatusCode.NoContent
			|| !response.Headers.TryGetValues("X-Terraform-Get", out var downloadUrls))
		{
			throw new ModuleNotFoundException(moduleReference);
		}

		var urlString = downloadUrls.FirstOrDefault();
		if (string.IsNullOrWhiteSpace(urlString))
			throw new ModuleNotFoundException(moduleReference);

		var urlDownload = new Uri(urlString);
		var download = await HttpRequest(HttpMethod.Get, urlDownload, true);
		var filePath = new FileInfo($"{path.FullName}/{moduleReference.FullName}.zip");
		var writeStream = File.OpenWrite(filePath.FullName);
		var readStream = await download.Content.ReadAsStreamAsync();
		await readStream.CopyToAsync(writeStream);
		writeStream.Close();
		readStream.Close();
		ZipFile.ExtractToDirectory(filePath.FullName, filePath.DirectoryName!);
		filePath.Delete();
	}
	private async Task<HttpResponseMessage> HttpRequest(HttpMethod method, string path, bool justHeaders = false)
		=> await HttpRequest(method, new UriBuilder(Endpoint) { Path = path }.Uri, justHeaders);
	private async Task<HttpResponseMessage> HttpRequest(HttpMethod method, Uri uri, bool justHeaders = false)
	{
		using var client = new HttpClient();
		var request = new HttpRequestMessage(method, uri);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", (await GetTokenAsync()).RawData);
		var completionOption = justHeaders ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead;
		var response = await client.SendAsync(request, completionOption);
		return response;
	}
}
