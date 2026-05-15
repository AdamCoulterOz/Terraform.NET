using System.Text.Json.Nodes;
using FluentAssertions;
using TF.Azure.Credentials;
using TF.Azure.Providers;
using TF.PowerPlatform.Credentials;
using TF.PowerPlatform.Providers;
using Xunit;

namespace TF.Tests.Unit;

public class ProviderConfigurationRewriterTests : IDisposable
{
	private readonly DirectoryInfo _root;

	public ProviderConfigurationRewriterTests()
	{
		var path = Path.Join(Path.GetTempPath(), Path.GetRandomFileName().Replace(".", ""));
		_root = Directory.CreateDirectory(path);
	}

	[Fact]
	public void Rewrite_ShouldExtractHclProviderBlocks_AndMergeBoundSettings()
	{
		var rootConfig = Path.Join(_root.FullName, "root.tf");
		File.WriteAllText(rootConfig, @"provider ""azurerm"" {
  features {}
  skip_provider_registration = true
  subscription_id = ""00000000-0000-0000-0000-000000000000""
}

provider ""azurerm"" {
  alias = ""prod""
  features {}
  skip_provider_registration = false
  subscription_id = ""00000000-0000-0000-0000-000000000001""
}

resource ""null_resource"" ""example"" {}");

		var providers = new ProviderCollection();
		providers.SetDefault(new AzureProvider(
			Guid.Parse("11111111-1111-1111-1111-111111111111"),
			new AzureSPSecretCredential(
				Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
				Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
				"default-secret")));

		providers.SetAlias("prod", new AzureProvider(
			Guid.Parse("22222222-2222-2222-2222-222222222222"),
			new AzureSPSecretCredential(
				Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
				Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
				"prod-secret")));

		ProviderConfigurationRewriter.Rewrite(_root, providers);

		var rewrittenRoot = File.ReadAllText(rootConfig);
		rewrittenRoot.Should().NotContain("provider \"azurerm\"");
		rewrittenRoot.Should().Contain("resource \"null_resource\" \"example\" {}");

		var generated = JsonNode.Parse(File.ReadAllText(Path.Join(_root.FullName, ProviderConfigurationRewriter.GeneratedFileName)))!;
		generated["terraform"]!["required_providers"]!["azurerm"]!["source"]!.GetValue<string>().Should().Be("hashicorp/azurerm");
		var blocks = generated["provider"]!["azurerm"]!.AsArray();
		blocks.Should().HaveCount(2);

		var defaultBlock = blocks.Single(block => block?["alias"] is null)!;
		defaultBlock["features"].Should().BeOfType<JsonObject>();
		defaultBlock["skip_provider_registration"]!.GetValue<bool>().Should().BeTrue();
		defaultBlock["subscription_id"]!.GetValue<string>().Should().Be("11111111-1111-1111-1111-111111111111");
		defaultBlock["tenant_id"]!.GetValue<string>().Should().Be("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
		defaultBlock["client_id"]!.GetValue<string>().Should().Be("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
		defaultBlock["client_secret"]!.GetValue<string>().Should().Be("default-secret");

		var prodBlock = blocks.Single(block => block?["alias"]?.GetValue<string>() == "prod")!;
		prodBlock["skip_provider_registration"]!.GetValue<bool>().Should().BeFalse();
		prodBlock["subscription_id"]!.GetValue<string>().Should().Be("22222222-2222-2222-2222-222222222222");
		prodBlock["tenant_id"]!.GetValue<string>().Should().Be("cccccccc-cccc-cccc-cccc-cccccccccccc");
		prodBlock["client_id"]!.GetValue<string>().Should().Be("dddddddd-dddd-dddd-dddd-dddddddddddd");
		prodBlock["client_secret"]!.GetValue<string>().Should().Be("prod-secret");
	}

	[Fact]
	public void Rewrite_ShouldExtractProviderBlocksFromTerraformJson_AndPreserveOtherContent()
	{
		var rootConfig = Path.Join(_root.FullName, "root.tf.json");
		File.WriteAllText(rootConfig, @"{
  ""provider"": {
    ""azurerm"": {
      ""features"": {},
      ""skip_provider_registration"": true
    }
  },
  ""resource"": {
    ""null_resource"": {
      ""example"": {}
    }
  }
}");

		ProviderConfigurationRewriter.Rewrite(_root, new ProviderCollection());

		var rewrittenRoot = JsonNode.Parse(File.ReadAllText(rootConfig))!;
		rewrittenRoot["provider"].Should().BeNull();
		rewrittenRoot["resource"]!["null_resource"]!["example"].Should().NotBeNull();

		var generated = JsonNode.Parse(File.ReadAllText(Path.Join(_root.FullName, ProviderConfigurationRewriter.GeneratedFileName)))!;
		generated["terraform"]!["required_providers"]!["azurerm"]!["source"]!.GetValue<string>().Should().Be("hashicorp/azurerm");
		var azurerm = generated["provider"]!["azurerm"]!;
		azurerm["features"].Should().BeOfType<JsonObject>();
		azurerm["skip_provider_registration"]!.GetValue<bool>().Should().BeTrue();
	}

	[Fact]
	public void Rewrite_ShouldEmitBoundProviderSourceAddresses()
	{
		var providers = new ProviderCollection();
		providers.SetDefault(new AzApiProvider(new AzureSPSecretCredential(
			Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
			Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
			"secret")));

		ProviderConfigurationRewriter.Rewrite(_root, providers);

		var generated = JsonNode.Parse(File.ReadAllText(Path.Join(_root.FullName, ProviderConfigurationRewriter.GeneratedFileName)))!;
		generated["terraform"]!["required_providers"]!["azapi"]!["source"]!.GetValue<string>().Should().Be("azure/azapi");
		generated["provider"]!["azapi"]!["tenant_id"]!.GetValue<string>().Should().Be("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
	}

	[Fact]
	public void Rewrite_ShouldUseProviderSpecificOidcFieldNames()
	{
		var tenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
		var clientId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
		var providers = new ProviderCollection();
		providers.SetAlias("azapi", new AzApiProvider(new AzureOidcCredential(
			tenantId,
			clientId,
			oidcRequestToken: "request-token",
			oidcRequestUrl: new Uri("https://pipelines.example.test/oidc"),
			azureDevOpsServiceConnectionId: "service-connection")));
		providers.SetAlias("power", new PowerPlatformProvider(new PowerPlatformOidcCredential(
			tenantId,
			clientId,
			oidcRequestUri: new Uri("https://pipelines.example.test/oidc"),
			azureDevOpsServiceConnectionId: "service-connection")));

		ProviderConfigurationRewriter.Rewrite(_root, providers);

		var generated = JsonNode.Parse(File.ReadAllText(Path.Join(_root.FullName, ProviderConfigurationRewriter.GeneratedFileName)))!;
		var azapi = ProviderBlock(generated, "azapi");
		azapi["oidc_request_url"]!.GetValue<string>().Should().Be("https://pipelines.example.test/oidc");
		azapi["ado_pipeline_service_connection_id"].Should().BeNull();

		var powerPlatform = ProviderBlock(generated, "power-platform");
		powerPlatform["oidc_request_url"]!.GetValue<string>().Should().Be("https://pipelines.example.test/oidc");
		powerPlatform["azdo_service_connection_id"]!.GetValue<string>().Should().Be("service-connection");
		powerPlatform["oidc_request_uri"].Should().BeNull();
		powerPlatform["ado_service_connection_id"].Should().BeNull();
	}

	[Fact]
	public void Rewrite_ShouldIgnoreTerraformJsonWithoutProviderBlock()
	{
		var rootConfig = Path.Join(_root.FullName, "root.tf.json");
		File.WriteAllText(rootConfig, @"{
  ""resource"": {
    ""null_resource"": {
      ""example"": {}
    }
  }
}");

		ProviderConfigurationRewriter.Rewrite(_root, new ProviderCollection());

		var rewrittenRoot = JsonNode.Parse(File.ReadAllText(rootConfig))!;
		rewrittenRoot["resource"]!["null_resource"]!["example"].Should().NotBeNull();
		File.Exists(Path.Join(_root.FullName, ProviderConfigurationRewriter.GeneratedFileName)).Should().BeFalse();
	}

	public void Dispose()
	{
		if (_root.Exists)
			_root.Delete(true);
	}

	private static JsonNode ProviderBlock(JsonNode generated, string providerName)
	{
		var provider = generated["provider"]![providerName]!;
		return provider is JsonArray array ? array.Single()! : provider;
	}
}
