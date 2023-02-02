using FluentAssertions;
using Xunit;


namespace TF.Tests.Unit;

public class ProvisioningTests
{
	private readonly DirectoryInfo _testDir;
	private readonly Terraform? _sut;

	public ProvisioningTests()
	{
		var dir = Path.Join(Path.GetTempPath(), Path.GetRandomFileName().Replace(".", ""));
		_testDir = Directory.CreateDirectory(dir);
		_sut = new() { Path = _testDir };
	}

	[Fact]
	public async Task Finalizer_ShouldCleanUpRootPath_WhenGarbageCollectedAsync()
	{
		// Arrange
		File.Copy("./ProvisioningTest.tf", Path.Join(_testDir.FullName, "ProvisioningTest.tf"));
		var initResult = await _sut!.Init();
		initResult.Should().NotBeNull();
		_testDir.Exists.Should().BeTrue();

		// Asset
		_testDir.Exists.Should().BeFalse();
	}
}
