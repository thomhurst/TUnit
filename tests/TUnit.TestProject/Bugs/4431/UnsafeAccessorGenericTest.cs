#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace TUnit.TestProject.Bugs._4431;

/// <summary>
/// Minimal reproduction test for UnsafeAccessor with generic types.
/// This test demonstrates that UnsafeAccessor does NOT work with fields on generic base classes.
/// The reflection-based approach works correctly.
/// </summary>
public class UnsafeAccessorGenericTest
{
    [Test]
    [Category("KnownLimitation")]
    public async Task UnsafeAccessor_FailsWithGenericBaseClass()
    {
        // Create an instance of the derived class
        var derivedInstance = new NonGenericClassDataSourceWithGenericBase_First_4431();
        var providerValue = new ProviderWithClassDataSource4431();

        // UnsafeAccessor does NOT work with fields on generic base classes
        // This is a known .NET limitation
        var ex = Assert.Throws<MissingFieldException>(() =>
        {
            UnsafeAccessorHelper.SetProviderField(derivedInstance, providerValue);
        });

        // The error message shows the open generic type (with backtick notation)
        await Assert.That(ex!.Message).Contains("GenericBaseWithInferredClassDataSource");
    }

    [Test]
    public async Task Reflection_ShouldWork_WithGenericBaseClass()
    {
        // Create an instance of the derived class
        var derivedInstance = new NonGenericClassDataSourceWithGenericBase_First_4431();
        var providerValue = new ProviderWithClassDataSource4431();

        // Reflection works correctly with the closed generic type
        var closedGenericBaseType = typeof(GenericBaseWithInferredClassDataSource<ProviderWithClassDataSource4431>);
        var backingField = closedGenericBaseType.GetField("<Provider>k__BackingField",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        await Assert.That(backingField).IsNotNull();

        backingField!.SetValue(derivedInstance, providerValue);

        await Assert.That(derivedInstance.Provider).IsNotNull();
        await Assert.That(derivedInstance.Provider).IsSameReferenceAs(providerValue);
    }
}

internal static class UnsafeAccessorHelper
{
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<Provider>k__BackingField")]
    internal static extern ref ProviderWithClassDataSource4431 GetProviderBackingField(
        GenericBaseWithInferredClassDataSource<ProviderWithClassDataSource4431> instance);

    public static void SetProviderField(object instance, ProviderWithClassDataSource4431 value)
    {
        var typedInstance = (GenericBaseWithInferredClassDataSource<ProviderWithClassDataSource4431>)instance;
        GetProviderBackingField(typedInstance) = value;
    }
}
#endif
