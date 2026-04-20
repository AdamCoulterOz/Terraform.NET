using System.Text.Json.Nodes;
using FluentAssertions;
using TF.Azure.Credentials;
using TF.Azure.Providers;
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
		var azurerm = generated["provider"]!["azurerm"]!;
		azurerm["features"].Should().BeOfType<JsonObject>();
		azurerm["skip_provider_registration"]!.GetValue<bool>().Should().BeTrue();
	}

	public void Dispose()
	{
		if (_root.Exists)
			_root.Delete(true);
	}
}
