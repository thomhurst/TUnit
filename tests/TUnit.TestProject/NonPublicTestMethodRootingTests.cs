using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

/// <summary>
/// Non-public test methods whose parameters resolve through ParameterMetadataFactory.ForNonPublicMethod.
/// The generator roots each one with [DynamicDependency] and its exact signature, so a private overload with the
/// same name (here one with a [DynamicallyAccessedMembers] parameter) is not kept by trimming.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class NonPublicTestMethodRootingTests
{
    [Test]
    [Arguments(1, "one")]
    internal async Task Internal_Method(int value, string text)
    {
        var parameters = TestContext.Current!.Metadata.TestDetails.MethodMetadata.Parameters;

        await Assert.That(parameters[0].ReflectionInfo.Name).IsEqualTo(nameof(value));
        await Assert.That(parameters[1].ReflectionInfo.Name).IsEqualTo(nameof(text));
        await Assert.That(parameters[0].ReflectionInfo.Member.Name).IsEqualTo(nameof(Internal_Method));
    }

    [Test]
    [Arguments(2)]
    internal async Task Internal_Generic<T>(T value)
    {
        var parameter = TestContext.Current!.Metadata.TestDetails.MethodMetadata.Parameters[0];

        await Assert.That(parameter.ReflectionInfo.Name).IsEqualTo(nameof(value));
        await Assert.That(((MethodInfo) parameter.ReflectionInfo.Member).IsGenericMethodDefinition).IsTrue();
    }

    [Test]
    [Arguments(3)]
    internal async Task Internal_Shares_Name(int value)
    {
        var parameter = TestContext.Current!.Metadata.TestDetails.MethodMetadata.Parameters[0];

        await Assert.That(parameter.ReflectionInfo.Name).IsEqualTo(nameof(value));
        await Assert.That(parameter.ReflectionInfo.Member.Name).IsEqualTo(nameof(Internal_Shares_Name));
        await Assert.That(Internal_Shares_Name(typeof(NonPublicTestMethodRootingTests))).IsGreaterThanOrEqualTo(0);
    }

    // Declared before the test below so reflection enumerates it first: the by-ref overload must not be mistaken
    // for the test method just because its element type matches.
    private void Internal_Ref_Overload(ref int value)
    {
        value++;
    }

    [Test]
    [Arguments(4)]
    internal async Task Internal_Ref_Overload(int value)
    {
        var parameter = TestContext.Current!.Metadata.TestDetails.MethodMetadata.Parameters[0];

        await Assert.That(parameter.ReflectionInfo.ParameterType).IsEqualTo(typeof(int));
        await Assert.That(parameter.ReflectionInfo.Member.Name).IsEqualTo(nameof(Internal_Ref_Overload));
    }

    private int Internal_Shares_Name(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicFields)] Type type)
    {
        return type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Length;
    }
}
