using FluentAssertions;
using TF.Azure.Credentials;
using TF.Azure.Providers;
using TF.AzureDevOps.Providers;
using TF.Fabric.Credentials;
using TF.Fabric.Providers;
using TF.PowerPlatform.Credentials;
using TF.PowerPlatform.Providers;
using Xunit;

namespace TF.Tests.Unit;

public class ProviderAuthTests
{
    [Fact]
    public void CombinedProviderConfigs_ShouldIncludeProviderEnvironmentValues()
    {
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var clientId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var subscriptionId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var providers = new ProviderCollection();
        providers.SetDefault(new AzureProvider(subscriptionId, new AzureOidcCredential(
            tenantId,
            clientId,
            oidcRequestToken: "request-token",
            oidcRequestUrl: new Uri("https://pipelines.example.test/oidc"),
            azureDevOpsServiceConnectionId: "service-connection")));

        providers.CombinedProviderConfigs.Should().Contain(new Dictionary<string, string>
        {
            ["ARM_TENANT_ID"] = tenantId.ToString(),
            ["ARM_CLIENT_ID"] = clientId.ToString(),
            ["ARM_SUBSCRIPTION_ID"] = subscriptionId.ToString(),
            ["ARM_USE_OIDC"] = "true",
            ["ARM_OIDC_REQUEST_TOKEN"] = "request-token",
            ["ARM_OIDC_REQUEST_URL"] = "https://pipelines.example.test/oidc",
            ["ARM_ADO_PIPELINE_SERVICE_CONNECTION_ID"] = "service-connection"
        });
    }

    [Fact]
    public void ProviderBindings_ShouldModelPlatformProviders()
    {
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var clientId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var providers = new ProviderCollection();
        providers.SetDefault(new AzureAdProvider(new AzureOidcCredential(tenantId, clientId, oidcToken: "token")));
        providers.SetAlias("azapi", new AzApiProvider(new AzureOidcCredential(tenantId, clientId, oidcToken: "token")));
        providers.SetAlias("devops", new AzureDevOpsProvider(new Uri("https://dev.azure.com/example"), new AzureOidcCredential(tenantId, clientId, oidcToken: "token")));
        providers.SetAlias("fabric", new FabricProvider(new FabricOidcCredential(tenantId, clientId, oidcToken: "token")));
        providers.SetAlias("power", new PowerPlatformProvider(new PowerPlatformOidcCredential(tenantId, clientId, oidcToken: "token")));

        providers.Bindings.Select(binding => binding.ProviderName).Should().Contain(["azuread", "azapi", "azuredevops", "fabric", "power-platform"]);
        providers.CombinedProviderConfigs.Should().ContainKey("POWER_PLATFORM_USE_OIDC");
        providers.CombinedProviderConfigs.Should().ContainKey("AZDO_ORG_SERVICE_URL");
        providers.CombinedProviderConfigs.Should().ContainKey("FABRIC_USE_OIDC");
    }

    [Fact]
    public void EntraOidcCredentials_ShouldExposeSharedSemanticAuthShape()
    {
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var clientId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        IEntraOidcCredential azure = new AzureOidcCredential(
            tenantId,
            clientId,
            oidcRequestToken: "request-token",
            oidcRequestUrl: new Uri("https://pipelines.example.test/oidc"),
            azureDevOpsServiceConnectionId: "service-connection");
        IEntraOidcCredential fabric = new FabricOidcCredential(tenantId, clientId, oidcToken: "fabric-token");
        IEntraOidcCredential powerPlatform = new PowerPlatformOidcCredential(
            tenantId,
            clientId,
            oidcToken: "power-token",
            oidcRequestUri: new Uri("https://pipelines.example.test/power-oidc"),
            azureDevOpsServiceConnectionId: "power-service-connection");

        azure.TenantId.Should().Be(tenantId);
        azure.ClientId.Should().Be(clientId);
        azure.UseOidc.Should().BeTrue();
        azure.OidcRequestToken.Should().Be("request-token");
        azure.OidcRequestUrl.Should().Be(new Uri("https://pipelines.example.test/oidc"));
        azure.AzureDevOpsServiceConnectionId.Should().Be("service-connection");

        fabric.OidcToken.Should().Be("fabric-token");
        fabric.OidcRequestUrl.Should().BeNull();
        fabric.AzureDevOpsServiceConnectionId.Should().BeNull();

        powerPlatform.OidcToken.Should().Be("power-token");
        powerPlatform.OidcRequestUrl.Should().Be(new Uri("https://pipelines.example.test/power-oidc"));
        powerPlatform.AzureDevOpsServiceConnectionId.Should().Be("power-service-connection");
    }

    [Fact]
    public void Terraform_ShouldNotDeleteExternalWorkingDirectory()
    {
        var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        using (Terraform.ForExternalWorkingDirectory(new TF.BuiltIn.LocalBackend(), directory))
        {
        }

        directory.Exists.Should().BeTrue();
        directory.Delete(recursive: true);
    }
}
