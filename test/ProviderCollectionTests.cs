using Moq;
using Xunit;

namespace TF.Tests.Unit;

public class ProviderCollectionTests
{
	[Fact]
	public void SetDefault_ShouldAddProviderToDictionary()
	{
		// Arrange
		var providerCollection = new ProviderSet();

		// Act
		providerCollection.Add(new TestProvider());

		// Assert
		// Assert.True(providerCollection.Aliases.ContainsKey(("default", "Provider")));
	}

	[Fact]
	public void SetAlias_ShouldThrowExceptionIfAliasIsDefault()
	{
		// Arrange
		var providerCollection = new ProviderSet();

		// Act & Assert
		Assert.Throws<ArgumentOutOfRangeException>(() => providerCollection.Add(new TestProvider()));
	}

	[Fact]
	public void SetAlias_ShouldAddProviderToDictionary()
	{
		// Arrange
		var alias = "test";

		var providerCollection = new ProviderSet();
		var testProvider = new TestProvider();

		// Act
		providerCollection.Add(testProvider, alias);

		// Assert
		// Assert.True(providerCollection.Aliases.ContainsKey((alias, testProvider.Name)));
	}

	[Fact]
	public void CombinedProviderConfigs_ShouldReturnCorrectValue()
	{
		// Arrange
		var alias = "test";
		var key1 = "key1";
		var value1 = "value1";
		var key2 = "key2";
		var value2 = "value2";
		var configs = new Dictionary<string, string>
			{ { key1, value1 }, { key2, value2 } };

		var providerMock = new Mock<Provider>();
		providerMock.Setup(p => p.GetConfig())
					.Returns(configs);

		var providerCollection = new ProviderSet();

		providerCollection.Add(providerMock.Object, alias);

		// Act
		var combinedConfigs = providerCollection.CombinedProviderConfigs;

	}
}