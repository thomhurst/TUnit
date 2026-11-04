using System.Reflection;

namespace TUnit.Core.SourceGenerator.Tests;

internal class ReflectionDeclaredMemberTests
{
    public static IEnumerable<int> AmbiguousRows(int value) => [value];
    public static IEnumerable<string> AmbiguousRows(string value) => [value];
    public static void GenericTarget<T>(T value) { }

    [Test]
    public async Task Ambiguous_member_does_not_abort_generic_method_discovery()
    {
        var closeMethod = typeof(TUnit.Engine.Framework.TestingPlatformBuilderHook).Assembly
            .GetType("TUnit.Engine.Discovery.ReflectionTestDataCollector", throwOnError: true)!
            .GetMethod("CloseOverDeclaredTypedSourceTypes", BindingFlags.NonPublic | BindingFlags.Static)!;
        var closed = (MethodInfo?)closeMethod.Invoke(null,
            [typeof(ReflectionDeclaredMemberTests), typeof(ReflectionDeclaredMemberTests).GetMethod(nameof(GenericTarget))!,
                new TUnit.Core.MethodDataSourceAttribute(nameof(AmbiguousRows))]);
        await Assert.That(closed).IsNull();
    }
}
