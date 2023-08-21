using NSubstitute;
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
		Assert.True(providerCollection.TryGet(out _, "test"));
	}

	[Fact]
	public void SetAlias_ShouldThrowExceptionIfAliasIsDefault()
	{
		// Arrange
		var providerCollection = new ProviderSet();

		// Act & Assert
		providerCollection.Add(new TestProvider());
		Assert.Throws<ArgumentException>(() => providerCollection.Add(new TestProvider()));
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
		Assert.True(providerCollection.TryGet(out _, testProvider.Name, alias));
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

		
		var providerMock = Substitute.For<IProvider>(); // Substitute the interface
		providerMock.GetConfig().Returns(configs);
		providerMock.Name.Returns("ProviderName"); // You may need to set a value for the Name property


		var providerCollection = new ProviderSet();

		providerCollection.Add(providerMock, alias);

		// Act
		var combinedConfigs = providerCollection.CombinedProviderConfigs;

		// Assert
		var key = $"{providerMock.Name}.{alias}.{key1}";
		var matchTo = combinedConfigs[key];
		Assert.Equal(value1, matchTo);
	}
}
