using System.Reflection;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class AssemblyAssertionTests
{
    [Test]
    public async Task Test_Assembly_IsDynamic()
    {
        // System assemblies are not dynamic
        var assembly = typeof(object).Assembly;
        await Assert.That(assembly).IsNotDynamic();
    }

    [Test]
    public async Task Test_Assembly_IsNotDynamic()
    {
        var assembly = typeof(AssemblyAssertionTests).Assembly;
        await Assert.That(assembly).IsNotDynamic();
    }

    [Test]
    public async Task Test_Assembly_IsFullyTrusted()
    {
        var assembly = typeof(AssemblyAssertionTests).Assembly;
        await Assert.That(assembly).IsFullyTrusted();
    }

    [Test]
    public async Task Test_Assembly_IsFullyTrusted_SystemAssembly()
    {
        var assembly = typeof(object).Assembly;
        await Assert.That(assembly).IsFullyTrusted();
    }

    [Test]
    public async Task Test_Assembly_IsSigned_SystemAssembly()
    {
        // System assemblies are signed with strong names
        var assembly = typeof(object).Assembly;
        await Assert.That(assembly).IsSigned();
    }

    [Test]
    public async Task Test_Assembly_IsNotSigned_TestAssembly()
    {
        // Test assemblies typically aren't signed
        var assembly = typeof(AssemblyAssertionTests).Assembly;
        await Assert.That(assembly).IsNotSigned();
    }

#if DEBUG
    [Test]
    public async Task Test_Assembly_IsDebugBuild()
    {
        // This test project is built in debug mode
        var assembly = typeof(AssemblyAssertionTests).Assembly;
        await Assert.That(assembly).IsDebugBuild();
    }
#else
    [Test]
    public async Task Test_Assembly_IsReleaseBuild()
    {
        // This test project is built in release mode
        var assembly = typeof(AssemblyAssertionTests).Assembly;
        await Assert.That(assembly).IsReleaseBuild();
    }
#endif

#if !NETSTANDARD2_0
    [Test]
    public async Task Test_Assembly_IsNotCollectible()
    {
        // Standard assemblies are not collectible
        var assembly = typeof(AssemblyAssertionTests).Assembly;
        await Assert.That(assembly).IsNotCollectible();
    }

    [Test]
    public async Task Test_Assembly_IsNotCollectible_SystemAssembly()
    {
        var assembly = typeof(object).Assembly;
        await Assert.That(assembly).IsNotCollectible();
    }
#endif
}
