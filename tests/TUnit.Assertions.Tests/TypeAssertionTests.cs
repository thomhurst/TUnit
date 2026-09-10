using System.Reflection;
using TUnit.Assertions.Core;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Tests;

public class TypeAssertionTests
{
    private class Animal { }
    private class Dog : Animal { }

    // Test types for various scenarios
    private class TestClass { }
    private interface ITestInterface { }
    private abstract class AbstractClass { }
    private sealed class SealedClass { }
    private struct TestStruct { }
    private enum TestEnum { Value1, Value2 }

    public class PublicClass { }
    private class PrivateClass { }

    private class OuterClass
    {
        public class NestedPublicClass { }
        private class NestedPrivateClass { }
        internal class NestedInternalClass { }
        protected class NestedProtectedClass { }
    }

    // IsClass / IsNotClass
    [Test]
    public async Task Test_Type_IsClass()
    {
        var type = typeof(TestClass);
        await Assert.That(type).IsClass();
    }

    [Test]
    public async Task Test_Type_IsClass_String()
    {
        var type = typeof(string);
        await Assert.That(type).IsClass();
    }

    [Test]
    public async Task Test_Type_IsNotClass_Interface()
    {
        var type = typeof(ITestInterface);
        await Assert.That(type).IsNotClass();
    }

    [Test]
    public async Task Test_Type_IsNotClass_Struct()
    {
        var type = typeof(TestStruct);
        await Assert.That(type).IsNotClass();
    }

    // IsInterface / IsNotInterface
    [Test]
    public async Task Test_Type_IsInterface()
    {
        var type = typeof(ITestInterface);
        await Assert.That(type).IsInterface();
    }

    [Test]
    public async Task Test_Type_IsInterface_IDisposable()
    {
        var type = typeof(IDisposable);
        await Assert.That(type).IsInterface();
    }

    [Test]
    public async Task Test_Type_IsNotInterface()
    {
        var type = typeof(TestClass);
        await Assert.That(type).IsNotInterface();
    }

    // IsAbstract / IsNotAbstract
    [Test]
    public async Task Test_Type_IsAbstract()
    {
        var type = typeof(AbstractClass);
        await Assert.That(type).IsAbstract();
    }

    [Test]
    public async Task Test_Type_IsAbstract_Interface()
    {
        // Interfaces are also abstract
        var type = typeof(ITestInterface);
        await Assert.That(type).IsAbstract();
    }

    [Test]
    public async Task Test_Type_IsNotAbstract()
    {
        var type = typeof(TestClass);
        await Assert.That(type).IsNotAbstract();
    }

    // IsSealed / IsNotSealed
    [Test]
    public async Task Test_Type_IsSealed()
    {
        var type = typeof(SealedClass);
        await Assert.That(type).IsSealed();
    }

    [Test]
    public async Task Test_Type_IsSealed_String()
    {
        var type = typeof(string);
        await Assert.That(type).IsSealed();
    }

    [Test]
    public async Task Test_Type_IsNotSealed()
    {
        var type = typeof(TestClass);
        await Assert.That(type).IsNotSealed();
    }

    // IsValueType / IsNotValueType
    [Test]
    public async Task Test_Type_IsValueType()
    {
        var type = typeof(TestStruct);
        await Assert.That(type).IsValueType();
    }

    [Test]
    public async Task Test_Type_IsValueType_Int()
    {
        var type = typeof(int);
        await Assert.That(type).IsValueType();
    }

    [Test]
    public async Task Test_Type_IsNotValueType()
    {
        var type = typeof(TestClass);
        await Assert.That(type).IsNotValueType();
    }

    // IsEnum / IsNotEnum
    [Test]
    public async Task Test_Type_IsEnum()
    {
        var type = typeof(TestEnum);
        await Assert.That(type).IsEnum();
    }

    [Test]
    public async Task Test_Type_IsEnum_DayOfWeek()
    {
        var type = typeof(DayOfWeek);
        await Assert.That(type).IsEnum();
    }

    [Test]
    public async Task Test_Type_IsNotEnum()
    {
        var type = typeof(TestClass);
        await Assert.That(type).IsNotEnum();
    }

    // IsPrimitive / IsNotPrimitive
    [Test]
    public async Task Test_Type_IsPrimitive_Int()
    {
        var type = typeof(int);
        await Assert.That(type).IsPrimitive();
    }

    [Test]
    public async Task Test_Type_IsPrimitive_Bool()
    {
        var type = typeof(bool);
        await Assert.That(type).IsPrimitive();
    }

    [Test]
    public async Task Test_Type_IsNotPrimitive_String()
    {
        var type = typeof(string);
        await Assert.That(type).IsNotPrimitive();
    }

    [Test]
    public async Task Test_Type_IsNotPrimitive_Decimal()
    {
        var type = typeof(decimal);
        await Assert.That(type).IsNotPrimitive();
    }

    // IsPublic / IsNotPublic
    [Test]
    public async Task Test_Type_IsPublic()
    {
        // Use string which is a top-level public type
        var type = typeof(string);
        await Assert.That(type).IsPublic();
    }

    [Test]
    public async Task Test_Type_IsPublic_String()
    {
        var type = typeof(string);
        await Assert.That(type).IsPublic();
    }

    [Test]
    public async Task Test_Type_IsNotPublic()
    {
        var type = typeof(PrivateClass);
        await Assert.That(type).IsNotPublic();
    }

    // IsGenericType / IsNotGenericType
    [Test]
    public async Task Test_Type_IsGenericType()
    {
        var type = typeof(List<int>);
        await Assert.That(type).IsGenericType();
    }

    [Test]
    public async Task Test_Type_IsGenericType_Dictionary()
    {
        var type = typeof(Dictionary<string, int>);
        await Assert.That(type).IsGenericType();
    }

    [Test]
    public async Task Test_Type_IsNotGenericType()
    {
        var type = typeof(string);
        await Assert.That(type).IsNotGenericType();
    }

    // IsGenericTypeDefinition / IsNotGenericTypeDefinition
    [Test]
    public async Task Test_Type_IsGenericTypeDefinition()
    {
        var type = typeof(List<>);
        await Assert.That(type).IsGenericTypeDefinition();
    }

    [Test]
    public async Task Test_Type_IsGenericTypeDefinition_Dictionary()
    {
        var type = typeof(Dictionary<,>);
        await Assert.That(type).IsGenericTypeDefinition();
    }

    [Test]
    public async Task Test_Type_IsNotGenericTypeDefinition()
    {
        var type = typeof(List<int>);
        await Assert.That(type).IsNotGenericTypeDefinition();
    }

    // IsArray / IsNotArray
    [Test]
    public async Task Test_Type_IsArray()
    {
        var type = typeof(int[]);
        await Assert.That(type).IsArray();
    }

    [Test]
    public async Task Test_Type_IsArray_String()
    {
        var type = typeof(string[]);
        await Assert.That(type).IsArray();
    }

    [Test]
    public async Task Test_Type_IsNotArray()
    {
        var type = typeof(List<int>);
        await Assert.That(type).IsNotArray();
    }

    // IsByRef / IsNotByRef
    [Test]
    public async Task Test_Type_IsByRef()
    {
        var method = typeof(TypeAssertionTests).GetMethod(nameof(RefMethod));
        var parameter = method!.GetParameters()[0];
        var type = parameter.ParameterType;
        await Assert.That(type).IsByRef();
    }

    public void RefMethod(ref int value) { }

    [Test]
    public async Task Test_Type_IsNotByRef()
    {
        var type = typeof(int);
        await Assert.That(type).IsNotByRef();
    }

    // IsPointer / IsNotPointer
    [Test]
    public async Task Test_Type_IsNotPointer()
    {
        var type = typeof(int);
        await Assert.That(type).IsNotPointer();
    }

    // IsNested / IsNotNested
    [Test]
    public async Task Test_Type_IsNested()
    {
        var type = typeof(OuterClass.NestedPublicClass);
        await Assert.That(type).IsNested();
    }

    [Test]
    public async Task Test_Type_IsNotNested()
    {
        // Use string which is a top-level type, not nested
        var type = typeof(string);
        await Assert.That(type).IsNotNested();
    }

    // IsNestedPublic / IsNotNestedPublic
    [Test]
    public async Task Test_Type_IsNestedPublic()
    {
        var type = typeof(OuterClass.NestedPublicClass);
        await Assert.That(type).IsNestedPublic();
    }

    [Test]
    public async Task Test_Type_IsNotNestedPublic()
    {
        var type = typeof(OuterClass).GetNestedType("NestedPrivateClass", System.Reflection.BindingFlags.NonPublic);
        await Assert.That(type!).IsNotNestedPublic();
    }

    // IsNestedPrivate / IsNotNestedPrivate
    [Test]
    public async Task Test_Type_IsNestedPrivate()
    {
        var type = typeof(OuterClass).GetNestedType("NestedPrivateClass", System.Reflection.BindingFlags.NonPublic);
        await Assert.That(type!).IsNestedPrivate();
    }

    [Test]
    public async Task Test_Type_IsNotNestedPrivate()
    {
        var type = typeof(OuterClass.NestedPublicClass);
        await Assert.That(type).IsNotNestedPrivate();
    }

    // IsNestedAssembly / IsNotNestedAssembly
    [Test]
    public async Task Test_Type_IsNestedAssembly()
    {
        var type = typeof(OuterClass).GetNestedType("NestedInternalClass", System.Reflection.BindingFlags.NonPublic);
        await Assert.That(type!).IsNestedAssembly();
    }

    [Test]
    public async Task Test_Type_IsNotNestedAssembly()
    {
        var type = typeof(OuterClass.NestedPublicClass);
        await Assert.That(type).IsNotNestedAssembly();
    }

    // IsNestedFamily / IsNotNestedFamily
    [Test]
    public async Task Test_Type_IsNestedFamily()
    {
        var type = typeof(OuterClass).GetNestedType("NestedProtectedClass", System.Reflection.BindingFlags.NonPublic);
        await Assert.That(type!).IsNestedFamily();
    }

    [Test]
    public async Task Test_Type_IsNotNestedFamily()
    {
        var type = typeof(OuterClass.NestedPublicClass);
        await Assert.That(type).IsNotNestedFamily();
    }

    // IsVisible / IsNotVisible
    [Test]
    public async Task Test_Type_IsVisible()
    {
        var type = typeof(string);
        await Assert.That(type).IsVisible();
    }

    [Test]
    public async Task Test_Type_IsVisible_PublicClass()
    {
        var type = typeof(PublicClass);
        await Assert.That(type).IsVisible();
    }

    [Test]
    public async Task Test_Type_IsNotVisible()
    {
        var type = typeof(PrivateClass);
        await Assert.That(type).IsNotVisible();
    }

    // IsConstructedGenericType / IsNotConstructedGenericType
    [Test]
    public async Task Test_Type_IsConstructedGenericType()
    {
        var type = typeof(List<int>);
        await Assert.That(type).IsConstructedGenericType();
    }

    [Test]
    public async Task Test_Type_IsConstructedGenericType_Dictionary()
    {
        var type = typeof(Dictionary<string, int>);
        await Assert.That(type).IsConstructedGenericType();
    }

    [Test]
    public async Task Test_Type_IsNotConstructedGenericType()
    {
        var type = typeof(List<>);
        await Assert.That(type).IsNotConstructedGenericType();
    }

    // ContainsGenericParameters / DoesNotContainGenericParameters
    [Test]
    public async Task Test_Type_ContainsGenericParameters()
    {
        var type = typeof(List<>);
        await Assert.That(type).ContainsGenericParameters();
    }

    [Test]
    public async Task Test_Type_DoesNotContainGenericParameters()
    {
        var type = typeof(List<int>);
        await Assert.That(type).DoesNotContainGenericParameters();
    }

    // IsCOMObject / IsNotCOMObject
    [Test]
    public async Task Test_Type_IsNotCOMObject()
    {
        var type = typeof(string);
        await Assert.That(type).IsNotCOMObject();
    }

    [Test]
    public async Task Test_Type_IsNotCOMObject_TestClass()
    {
        var type = typeof(TestClass);
        await Assert.That(type).IsNotCOMObject();
    }

    [Test]
    public async Task Test_Type_IsAssignableTo_Generic_UsesRepresentedType()
    {
        Type? result = await Assert.That(typeof(Dog)).IsAssignableTo<Animal>();

        await Assert.That(result).IsSameReferenceAs(typeof(Dog));
    }

    [Test]
    public async Task Test_Type_IsAssignableTo_Generic_RetainsTypeForChaining()
    {
        await Assert.That(typeof(Dog))
            .IsAssignableTo<Animal>()
            .And.IsClass();
    }

    [Test]
    public async Task Test_TypeInfo_GenericAssignability_UsesRepresentedType()
    {
        System.Reflection.TypeInfo animalType = typeof(Animal).GetTypeInfo();
        System.Reflection.TypeInfo dogType = typeof(Dog).GetTypeInfo();

        await Assert.That(dogType).IsAssignableTo<Animal>();
        await Assert.That(dogType).IsNotAssignableTo<Type>();
        await Assert.That(animalType).IsAssignableFrom<Dog>();
        await Assert.That(dogType).IsNotAssignableFrom<Animal>();
    }

    [Test]
    public async Task Test_Type_IsAssignableTo_GenericSource_RetainsRuntimeTypeSemantics()
    {
        IAssertionSource<Type> source = new TypeValueAssertion(typeof(Dog), null);

        await Assert.That(async () => await source.IsAssignableTo<Animal>())
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Test_Type_IsAssignableTo_AfterAnd_RetainsRuntimeTypeSemantics()
    {
        var action = async () => await Assert.That(typeof(Dog))
            .IsClass()
            .And.IsAssignableTo<Animal>();

        await Assert.That(action).Throws<AssertionException>();
    }

    [Test]
    public async Task Test_Type_OtherGenericSources_RetainRuntimeTypeSemantics()
    {
        IAssertionSource<Type> source = new TypeValueAssertion(typeof(Animal), null);

        await source.IsNotAssignableTo<Animal>();
        await Assert.That(async () => await source.IsAssignableFrom<Dog>())
            .Throws<AssertionException>();
        await source.IsNotAssignableFrom<Dog>();
    }

    [Test]
    public async Task Test_Type_DirectGenericAssertions_UseRepresentedType()
    {
        await Assert.That(typeof(Animal)).IsNotAssignableTo<Type>();
        await Assert.That(typeof(Animal)).IsAssignableFrom<Dog>();
        await Assert.That(async () => await Assert.That(typeof(Animal)).IsNotAssignableFrom<Dog>())
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Test_Type_IsAssignableFrom_Generic_UsesRepresentedType()
    {
        await Assert.That(typeof(Animal)).IsAssignableFrom<Dog>();
    }

    [Test]
    public async Task Test_Type_IsAssignableTo_RuntimeTypeOverload_Passes()
    {
        await Assert.That(typeof(Dog)).IsAssignableTo(typeof(Animal));
    }

    [Test]
    public async Task Test_Type_IsAssignableFrom_RuntimeTypeOverload_Passes()
    {
        await Assert.That(typeof(Animal)).IsAssignableFrom(typeof(Dog));
    }

    [Test]
    public async Task Test_Type_IsAssignableTo_NullRuntimeType_FailsAssertion()
    {
        var action = async () => await Assert.That(typeof(Dog)).IsAssignableTo(null!);

        var exception = await Assert.That(action).Throws<AssertionException>();
        await Assert.That(exception.Message).Contains("expected type was null");
    }

    [Test]
    public async Task Test_Type_IsAssignableFrom_NullRuntimeType_FailsAssertion()
    {
        var action = async () => await Assert.That(typeof(Animal)).IsAssignableFrom(null!);

        var exception = await Assert.That(action).Throws<AssertionException>();
        await Assert.That(exception.Message).Contains("source type was null");
    }

#if NET5_0_OR_GREATER
    // IsByRefLike / IsNotByRefLike (NET5+)
    [Test]
    public async Task Test_Type_IsByRefLike_Span()
    {
        var type = typeof(Span<int>);
        await Assert.That(type).IsByRefLike();
    }

    [Test]
    public async Task Test_Type_IsByRefLike_ReadOnlySpan()
    {
        var type = typeof(ReadOnlySpan<byte>);
        await Assert.That(type).IsByRefLike();
    }

    [Test]
    public async Task Test_Type_IsNotByRefLike()
    {
        var type = typeof(int);
        await Assert.That(type).IsNotByRefLike();
    }
#endif

#if !NET5_0_OR_GREATER
    // IsSerializable / IsNotSerializable (pre-NET5)
    [Test]
    public async Task Test_Type_IsSerializable()
    {
        var type = typeof(Exception);
        await Assert.That(type).IsSerializable();
    }

    [Test]
    public async Task Test_Type_IsNotSerializable()
    {
        var type = typeof(TestClass);
        await Assert.That(type).IsNotSerializable();
    }
#endif

    // Tests for issue #3425: Assertions before type conversion are not checked
    [Test]
    public async Task IsTypeOf_WithAnd_PreviousAssertion_ShouldFail()
    {
        // Length().IsEqualTo(4) should fail because "123" has length 3
        var exception = await Assert.That(async () =>
        {
            var sut = "123";
            await Assert.That(sut)
                .Length().IsEqualTo(4)
                .And
                .IsTypeOf<string>()
                .And
                .Length().IsEqualTo(3);
        }).ThrowsException().And.HasMessageContaining("IsEqualTo(4)");
    }

    [Test]
    public async Task IsTypeOf_WithAnd_PreviousAssertion_ShouldPass()
    {
        // Use Length(lambda) to preserve string context for And chaining
        var sut = "123";
        await Assert.That(sut)
            .Length(l => l.IsEqualTo(3))
            .And
            .IsTypeOf<string>()
            .And
            .StartsWith("1");
    }

    [Test]
    public async Task IsTypeOf_WithAnd_ObjectToString()
    {
        // IsNotNull should be checked before IsTypeOf
        object? sut = "test";
        await Assert.That(sut)
            .IsNotNull()
            .And
            .IsTypeOf<string>()
            .And
            .Length().IsEqualTo(4);
    }

    [Test]
    public async Task IsTypeOf_WithAnd_NullCheck_ShouldFail()
    {
        // IsNotNull should fail
        var exception = await Assert.That(async () =>
        {
            object? sut = null;
            await Assert.That(sut)
                .IsNotNull()
                .And
                .IsTypeOf<string>();
        }).ThrowsException().And.HasMessageContaining("IsNotNull");
    }

    [Test]
    public async Task IsTypeOf_WithAnd_MultipleAssertionsBeforeTypeChange()
    {
        // All object assertions should be checked before type conversion
        object? sut = "hello";
        await Assert.That(sut)
            .IsNotNull()
            .And
            .IsNotEqualTo("world")
            .And
            .IsTypeOf<string>()
            .And
            .Length(l => l.IsEqualTo(5))
            .And
            .StartsWith("h");
    }

    [Test]
    public async Task IsTypeOf_WithAnd_SecondObjectAssertionFails()
    {
        // Second object assertion should fail before type conversion
        var exception = await Assert.That(async () =>
        {
            object? sut = "hello";
            await Assert.That(sut)
                .IsNotNull()
                .And
                .IsEqualTo("world")
                .And
                .IsTypeOf<string>();
        }).ThrowsException().And.HasMessageContaining("IsEqualTo");
    }

    [Test]
    public async Task IsTypeOf_WithAnd_AssertionAfterTypeChangeFails()
    {
        // Object assertions pass, but string assertion fails
        var exception = await Assert.That(async () =>
        {
            object? sut = "hello";
            await Assert.That(sut)
                .IsNotNull()
                .And
                .IsTypeOf<string>()
                .And
                .Length().IsEqualTo(10);
        }).ThrowsException().And.HasMessageContaining("IsEqualTo");
    }

    [Test]
    public async Task IsTypeOf_AssertMultiple_AllAssertionsChecked()
    {
        // In Assert.Multiple, all failing assertions should be captured
        var exception = await Assert.That(async () =>
        {
            using (Assert.Multiple())
            {
                object? sut = "test";
                await Assert.That(sut)
                    .IsNotNull()
                    .And
                    .IsEqualTo("wrong")
                    .And
                    .IsTypeOf<string>()
                    .And
                    .Length().IsEqualTo(10);
            }
        }).ThrowsException();

        // Should have recorded the IsEqualTo failure at minimum
        await Assert.That(exception.Message).Contains("IsEqualTo");
    }

    [Test]
    public async Task IsTypeOf_ComplexChain_AllChecked()
    {
        // Complex chain with multiple type changes
        object? sut = "12345";
        await Assert.That(sut)
            .IsNotNull()
            .And
            .IsTypeOf<string>()
            .And
            .Length(l => l.IsEqualTo(5))
            .And
            .StartsWith("1")
            .And
            .EndsWith("5");
    }

    [Test]
    public async Task IsTypeOf_GenericObject_ToSpecificType()
    {
        // Generic object assertions before narrowing to specific type
        object? sut = new List<int> { 1, 2, 3 };
        await Assert.That(sut)
            .IsNotNull()
            .And
            .IsTypeOf<List<int>>();

        // Assert on collection contents separately
        await Assert.That((List<int>)sut).Count().IsEqualTo(3);
    }

    [Test]
    public async Task IsTypeOf_GenericObject_TypeCheckFails()
    {
        // Previous assertion should be checked before type check fails
        var exception = await Assert.That(async () =>
        {
            object? sut = "string";
            await Assert.That(sut)
                .IsNotNull()
                .And
                .IsEqualTo("wrong")
                .And
                .IsTypeOf<int>();
        }).ThrowsException().And.HasMessageContaining("IsEqualTo");
    }
}
