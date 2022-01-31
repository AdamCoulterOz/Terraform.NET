using System.IO;
using TF.BuiltIn;
using FluentAssertions;
using Xunit;
using System.Threading.Tasks;
using System;

namespace TF.Tests.Unit;

public class ProvisioningTests
{
    private DirectoryInfo _testDir;
    private TF.Terraform? _sut;

    public ProvisioningTests()
    {
        var dir = Path.Join(Path.GetTempPath(), Path.GetRandomFileName().Replace(".", ""));
        _testDir = Directory.CreateDirectory(dir);
        _sut = new(new LocalBackend(), _testDir, "terraform");
    }

    [Fact]
    public async Task Finalizer_ShouldCleanUpRootPath_WhenGarbageCollectedAsync()
    {
        // Arrange
        File.Copy("./ProvisioningTest.tf", Path.Join(_testDir.FullName, "ProvisioningTest.tf"));
        var initResult = (await _sut!.Init());
        initResult.Should().NotBeNull();
        _testDir.Exists.Should().BeTrue();

        // Act
        _sut = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();

        // Asset
        _testDir.Exists.Should().BeFalse();
    }
}
