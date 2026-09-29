using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

/// <summary>
/// Verifies that ParameterMetadata.ReflectionInfo resolves to the correct ParameterInfo for
/// constructors, instance/static/generic methods, params arrays and nested types.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[Arguments("class-arg", 5)]
public class ParameterReflectionInfoTests(string first, int second)
{
    public string First => first;
    public int Second => second;

    [Test]
    public async Task Constructor_Parameters()
    {
        var parameters = TestContext.Current!.Metadata.TestDetails.MethodMetadata.Class.Parameters;

        await Assert.That(parameters.Length).IsEqualTo(2);
        await AssertParameter(parameters[0], nameof(first), 0, typeof(string));
        await AssertParameter(parameters[1], nameof(second), 1, typeof(int));
        await Assert.That(parameters[0].ReflectionInfo.Member is ConstructorInfo).IsTrue();
    }

    [Test]
    [Arguments(1, "two", 3.0)]
    public async Task Instance_Method(int a, string? b, double c)
    {
        var parameters = TestContext.Current!.Metadata.TestDetails.MethodMetadata.Parameters;

        await AssertParameter(parameters[0], nameof(a), 0, typeof(int));
        await AssertParameter(parameters[1], nameof(b), 1, typeof(string));
        await AssertParameter(parameters[2], nameof(c), 2, typeof(double));
        await Assert.That(parameters[0].ReflectionInfo.Member.Name).IsEqualTo(nameof(Instance_Method));
        await Assert.That(ReferenceEquals(parameters[0].ReflectionInfo.Member, parameters[2].ReflectionInfo.Member)).IsTrue();
    }

    [Test]
    [Arguments(1, 2, 3)]
    public async Task Params_Array(params int[] values)
    {
        var parameter = TestContext.Current!.Metadata.TestDetails.MethodMetadata.Parameters[0];

        await AssertParameter(parameter, nameof(values), 0, typeof(int[]));
        await Assert.That(parameter.IsParams).IsTrue();
    }

    [Test]
    [Arguments(7, "seven")]
    internal async Task Internal_Method(int value, string text)
    {
        var parameters = TestContext.Current!.Metadata.TestDetails.MethodMetadata.Parameters;

        await AssertParameter(parameters[0], nameof(value), 0, typeof(int));
        await AssertParameter(parameters[1], nameof(text), 1, typeof(string));
        await Assert.That(parameters[0].ReflectionInfo.Member.Name).IsEqualTo(nameof(Internal_Method));
    }

    [Test]
    public async Task Private_Helper_With_Annotated_Parameter()
    {
        // Regression for IL2111: generated metadata must not ask the trimmer to keep every non-public method of
        // the test class, or a private helper like this one is reported as accessed via reflection.
        await Assert.That(CountFields(typeof(ParameterReflectionInfoTests))).IsGreaterThanOrEqualTo(0);
    }

    private static int CountFields([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicFields)] Type type)
    {
        return type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Length;
    }

    [Test]
    public async Task Static_Method_Via_Factory()
    {
        var parameters = ParameterMetadataFactory.ForMethod(typeof(Helpers), nameof(Helpers.StaticHelper), true,
            ParameterMetadataFactory.Create(typeof(int), "value", new ConcreteType(typeof(int)), false),
            ParameterMetadataFactory.Create(typeof(string), "text", new ConcreteType(typeof(string)), false));

        await AssertParameter(parameters[0], "value", 0, typeof(int));
        await AssertParameter(parameters[1], "text", 1, typeof(string));
        await Assert.That(parameters[1].ReflectionInfo.Member.Name).IsEqualTo(nameof(Helpers.StaticHelper));
    }

    [Test]
    public async Task Open_Generic_Constructor_Matched_By_Parameter_Count()
    {
        var parameters = ParameterMetadataFactory.ForConstructor(typeof(GenericHolder<>), true,
            ParameterMetadataFactory.Create(typeof(object), "value", new ConcreteType(typeof(object)), false),
            ParameterMetadataFactory.Create(typeof(int), "count", new ConcreteType(typeof(int)), false));

        await Assert.That(parameters[0].ReflectionInfo.Name).IsEqualTo("value");
        await Assert.That(parameters[1].ReflectionInfo.Name).IsEqualTo("count");
        await Assert.That(parameters[1].ReflectionInfo.Member is ConstructorInfo).IsTrue();
        await Assert.That(parameters[0].ReflectionInfo.Member.DeclaringType).IsEqualTo(typeof(GenericHolder<>));
    }

    [Test]
    public async Task Generic_Method_Via_Factory()
    {
        var parameters = ParameterMetadataFactory.ForGenericMethod(typeof(Helpers), nameof(Helpers.GenericHelper),
            ParameterMetadataFactory.Create(typeof(object), "item", new ConcreteType(typeof(object)), false));

        await Assert.That(parameters[0].ReflectionInfo.Name).IsEqualTo("item");
        await Assert.That(parameters[0].ReflectionInfo.Member.Name).IsEqualTo(nameof(Helpers.GenericHelper));
    }

    [Test]
    public async Task Unresolvable_Member_Reports_Lookup_In_Exception()
    {
        var parameters = ParameterMetadataFactory.ForMethod(typeof(ParameterReflectionInfoTests), "DoesNotExist", false,
            ParameterMetadataFactory.Create(typeof(int), "value", new ConcreteType(typeof(int)), false));

        var exception = Assert.Throws<InvalidOperationException>(() => _ = parameters[0].ReflectionInfo);

        await Assert.That(exception.Message).Contains("DoesNotExist");
        await Assert.That(exception.Message).Contains(nameof(ParameterReflectionInfoTests));
    }

    public static class Helpers
    {
        public static void StaticHelper(int value, string text)
        {
        }

        public static void GenericHelper<T>(T item)
        {
        }
    }

    public sealed class GenericHolder<T>
    {
        public GenericHolder(T value, int count)
        {
        }
    }

    internal static async Task AssertParameter(ParameterMetadata parameter, string name, int position, Type type)
    {
        var info = parameter.ReflectionInfo;
        await Assert.That(info.Name).IsEqualTo(name);
        await Assert.That(info.Position).IsEqualTo(position);
        await Assert.That(info.ParameterType).IsEqualTo(type);
    }

    [EngineTest(ExpectedResult.Pass)]
    public class Nested
    {
        [Test]
        [Arguments("nested")]
        public async Task Nested_Method(string value)
        {
            var parameter = TestContext.Current!.Metadata.TestDetails.MethodMetadata.Parameters[0];

            await ParameterReflectionInfoTests.AssertParameter(parameter, nameof(value), 0, typeof(string));
            await Assert.That(parameter.ReflectionInfo.Member.DeclaringType).IsEqualTo(typeof(Nested));
        }
    }
}

[EngineTest(ExpectedResult.Pass)]
public class GenericMethodParameterReflectionInfoTests
{
    [Test]
    [Arguments(42)]
    public async Task Generic_Method<T>(T value)
    {
        var parameter = TestContext.Current!.Metadata.TestDetails.MethodMetadata.Parameters[0];

        await Assert.That(parameter.ReflectionInfo.Name).IsEqualTo(nameof(value));
        await Assert.That(parameter.ReflectionInfo.Position).IsEqualTo(0);
        await Assert.That(parameter.ReflectionInfo.Member.Name).IsEqualTo(nameof(Generic_Method));
    }
}
