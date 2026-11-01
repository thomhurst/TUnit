using System.Reflection;
using TUnit.Engine.Discovery;

#pragma warning disable TUnit0019 // Subject is inspected through reflection discovery, not run as a test

namespace TUnit.UnitTests;

/// <summary>
/// Resolving a generic method without [GenerateGenericTest] extracts its data sources to find typed sources'
/// declared types (#7206). An attribute that throws there must not escape: discovery enumerates the
/// instantiations outside its per-method failure boundary, so the exception would fail the whole assembly.
/// The open method is yielded with no sources instead, so BuildTestMetadata reports the failure for that method.
/// </summary>
public class DeclaredTypedSourceExtractionFailureTests
{
    [Test]
    public async Task Throwing_Attribute_Yields_Open_Method_Without_Throwing()
    {
        var method = typeof(Subject).GetMethod(nameof(Subject.Run), BindingFlags.Public | BindingFlags.Instance)!;

        var instantiations = ReflectionTestDataCollector.ResolveMethodInstantiations(typeof(Subject), method).ToArray();

        await Assert.That(instantiations).Count().IsEqualTo(1);
        await Assert.That(instantiations[0].Method).IsSameReferenceAs(method);
        await Assert.That(instantiations[0].FailedMetadata).IsNull();
        await Assert.That(instantiations[0].MethodArguments).IsNull();
    }

    public sealed class ThrowingDataSourceAttribute : DataSourceGeneratorAttribute<string>
    {
        public ThrowingDataSourceAttribute() => throw new InvalidOperationException("attribute constructor failed");

        protected override IEnumerable<Func<string>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
        {
            yield return () => "x";
        }
    }

    // Not a [Test]: only inspected through reflection discovery
    public class Subject
    {
        [ThrowingDataSource]
        public void Run<T>(T value)
        {
        }
    }
}
