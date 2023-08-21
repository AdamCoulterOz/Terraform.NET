using FluentAssertions;
using TF.Results;
using Xunit;


namespace TF.Tests.Unit;

public class ProvisioningTests
{

	[Fact]
	public static async Task Finalizer_ShouldCleanUpRootPath_WhenGarbageCollectedAsync()
	{
		// Arrange
		DirectoryInfo? testDir = null;

		// Act
		await using (var terraform = await Terraform.CreateAsync(new DirectoryInfo("./Samples/ProvisioningTest")))
		{
			testDir = terraform.Path;
			testDir.Exists.Should().BeTrue();
		}
		// Assert
		testDir.Exists.Should().BeFalse();
	}

	[Fact]
	public static async Task RunTerraformVersion()
	{
		// Arrange
		var currentArchitecture = SystemInfo.GetOSPlatform();

		// Act
		using var terraform = await Terraform.CreateAsync();;
		var result = await terraform.Version();

		// Assert
		string output = result switch
		{
			Successful<Model.Version> success => success.Data.Architecture,
			Failed<Model.Version> failure => failure.Error,
			_ => throw new InvalidOperationException()
		};

		Assert.IsType<Successful<Model.Version>>(result);
		Assert.Equal(currentArchitecture, result.Data.Architecture);
	}
}