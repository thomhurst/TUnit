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

    public class AmbiguousClass<T>
    {
        #pragma warning disable TUnit0019 // Metadata-only fixture; invoked by constraint-reporting regression below.
        [TUnit.Core.MethodDataSource(typeof(ReflectionDeclaredMemberTests), nameof(AmbiguousRows))]
        public void Run(T value) { }
        #pragma warning restore TUnit0019
    }

    [Test]
    public async Task Ambiguous_member_does_not_abort_class_constraint_reporting()
    {
        var violationsType = typeof(TUnit.Engine.Framework.TestingPlatformBuilderHook).Assembly
            .GetType("TUnit.Engine.Discovery.ReflectionGenericConstraintViolations", throwOnError: true)!;
        var violations = Activator.CreateInstance(violationsType,
            [typeof(AmbiguousClass<>), new[] { typeof(AmbiguousClass<>).GetMethod(nameof(AmbiguousClass<int>.Run))! }])!;
        violationsType.GetMethod("AddMethodRowViolations")!.Invoke(violations, [false]);
        var metadata = (System.Collections.IEnumerable)violationsType.GetMethod("CreateMetadata")!.Invoke(violations, null)!;
        await Assert.That(metadata.Cast<object>().Count()).IsEqualTo(0);
    }
}
