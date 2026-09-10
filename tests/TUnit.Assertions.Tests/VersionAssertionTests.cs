using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class VersionAssertionTests
{
    [Test]
    public async Task Test_Version_IsMajorVersion()
    {
        var version = new Version(1, 0);
        await Assert.That(version).IsMajorVersion();
    }

    [Test]
    public async Task Test_Version_IsMajorVersion_FromString()
    {
        var version = new Version("5.0");
        await Assert.That(version).IsMajorVersion();
    }

    [Test]
    public async Task Test_Version_IsMajorVersion_Major_Only()
    {
        var version = new Version(2, 0, 0, 0);
        await Assert.That(version).IsMajorVersion();
    }

    [Test]
    public async Task Test_Version_IsNotMajorVersion()
    {
        var version = new Version(1, 2);
        await Assert.That(version).IsNotMajorVersion();
    }

    [Test]
    public async Task Test_Version_IsNotMajorVersion_WithBuild()
    {
        var version = new Version(1, 0, 1);
        await Assert.That(version).IsNotMajorVersion();
    }

    [Test]
    public async Task Test_Version_IsNotMajorVersion_WithRevision()
    {
        var version = new Version(1, 0, 0, 1);
        await Assert.That(version).IsNotMajorVersion();
    }

    [Test]
    public async Task Test_Version_HasBuildNumber()
    {
        var version = new Version(1, 2, 3);
        await Assert.That(version).HasBuildNumber();
    }

    [Test]
    public async Task Test_Version_HasBuildNumber_WithRevision()
    {
        var version = new Version(1, 2, 3, 4);
        await Assert.That(version).HasBuildNumber();
    }

    [Test]
    public async Task Test_Version_HasBuildNumber_Zero()
    {
        var version = new Version(1, 0, 0);
        await Assert.That(version).HasBuildNumber();
    }

    [Test]
    public async Task Test_Version_HasNoBuildNumber()
    {
        var version = new Version(1, 2);
        await Assert.That(version).HasNoBuildNumber();
    }

    [Test]
    public async Task Test_Version_HasRevisionNumber()
    {
        var version = new Version(1, 2, 3, 4);
        await Assert.That(version).HasRevisionNumber();
    }

    [Test]
    public async Task Test_Version_HasRevisionNumber_Zero()
    {
        var version = new Version(1, 2, 3, 0);
        await Assert.That(version).HasRevisionNumber();
    }

    [Test]
    public async Task Test_Version_HasNoRevisionNumber()
    {
        var version = new Version(1, 2, 3);
        await Assert.That(version).HasNoRevisionNumber();
    }

    [Test]
    public async Task Test_Version_HasNoRevisionNumber_MajorMinorOnly()
    {
        var version = new Version(1, 2);
        await Assert.That(version).HasNoRevisionNumber();
    }
}
