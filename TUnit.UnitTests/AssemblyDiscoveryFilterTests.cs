using System.Reflection;
using System.Reflection.Emit;
using TUnit.Core;
using TUnit.Engine.Discovery;

namespace TUnit.UnitTests;

public class AssemblyDiscoveryFilterTests
{
    [Test]
    public async Task IsExcludedFromTestDiscovery_ReturnsTrue_WhenAssemblyHasAttribute()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("TUnit.UnitTests.SelfExcludedFromDiscovery"),
            AssemblyBuilderAccess.Run);

        var constructor = typeof(ExcludeFromTestDiscoveryAttribute).GetConstructor(Type.EmptyTypes)!;
        assembly.SetCustomAttribute(new CustomAttributeBuilder(constructor, []));

        await Assert.That(AssemblyDiscoveryFilter.IsExcludedFromTestDiscovery(assembly)).IsTrue();
    }

    [Test]
    public async Task IsExcludedFromTestDiscovery_ReturnsTrue_WhenAssemblyNameWasRegistered()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("TUnit.UnitTests.EntryExcludedFromDiscovery"),
            AssemblyBuilderAccess.Run);

        SourceRegistrar.ExcludeAssemblyFromDiscovery("TUnit.UnitTests.EntryExcludedFromDiscovery");

        await Assert.That(AssemblyDiscoveryFilter.IsExcludedFromTestDiscovery(assembly)).IsTrue();
    }

    [Test]
    public async Task IsExcludedFromTestDiscovery_ReturnsFalse_WhenAssemblyDoesNotHaveAttribute()
    {
        await Assert.That(AssemblyDiscoveryFilter.IsExcludedFromTestDiscovery(typeof(AssemblyDiscoveryFilterTests).Assembly)).IsFalse();
    }
}
