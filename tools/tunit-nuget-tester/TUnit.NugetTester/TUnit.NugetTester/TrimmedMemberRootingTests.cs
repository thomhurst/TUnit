using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.NugetTester;

/// <summary>
/// Regression tests for trimming roots added by generated test metadata.
/// Generated code must not ask the trimmer to keep every non-public method of a test class. If it does, a
/// private helper with a [DynamicallyAccessedMembers] parameter (like the one below) is reported as IL2111
/// "accessed via reflection", which fails AOT publishes that treat warnings as errors (TUnit 1.72.0 regression).
/// </summary>
public class TrimmedMemberRootingTests
{
    private int _field = 1;

    [Test]
    public async Task Private_Helper_With_Annotated_Parameter_Does_Not_Trigger_IL2111()
    {
        await Assert.That(CountInstanceFields(this, typeof(TrimmedMemberRootingTests))).IsGreaterThanOrEqualTo(_field);
    }

    [Test]
    [Arguments(7, "seven")]
    internal async Task Internal_Method_Parameters_Resolve(int value, string text)
    {
        var parameters = TestContext.Current!.Metadata.TestDetails.MethodMetadata.Parameters;

        await Assert.That(parameters[0].ReflectionInfo.Name).IsEqualTo(nameof(value));
        await Assert.That(parameters[1].ReflectionInfo.Name).IsEqualTo(nameof(text));
        await Assert.That(parameters[0].ReflectionInfo.Member.Name).IsEqualTo(nameof(Internal_Method_Parameters_Resolve));
    }

    [Test]
    [Arguments(3)]
    internal async Task Internal_Generic_Method_Parameters_Resolve<T>(T value)
    {
        // Generic non-public methods must also be rooted by name only; enumerating the class's methods would keep
        // CountInstanceFields and bring IL2111 back.
        var parameter = TestContext.Current!.Metadata.TestDetails.MethodMetadata.Parameters[0];

        await Assert.That(parameter.ReflectionInfo.Name).IsEqualTo(nameof(value));
        await Assert.That(parameter.ReflectionInfo.Member.Name).IsEqualTo(nameof(Internal_Generic_Method_Parameters_Resolve));
    }

    [Test]
    [Arguments(4)]
    internal async Task Shares_Name_With_Annotated_Helper(int value)
    {
        // A private overload with the same name has a [DynamicallyAccessedMembers] parameter. Rooting by name
        // (any GetMethod(name, ...) intrinsic) would keep it too and report IL2111.
        var parameter = TestContext.Current!.Metadata.TestDetails.MethodMetadata.Parameters[0];

        await Assert.That(parameter.ReflectionInfo.Name).IsEqualTo(nameof(value));
        await Assert.That(Shares_Name_With_Annotated_Helper(this, typeof(TrimmedMemberRootingTests))).IsGreaterThanOrEqualTo(_field);
    }

    private int Shares_Name_With_Annotated_Helper(object owner,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicFields)] Type ownerType)
    {
        return CountInstanceFields(owner, ownerType);
    }

    private static int CountInstanceFields(object owner,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicFields)] Type ownerType)
    {
        return ownerType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Length + (owner is null ? -1 : 0);
    }
}
