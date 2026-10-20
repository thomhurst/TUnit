using System.Reflection;
using TUnit.Engine.Discovery;

#pragma warning disable TUnit0001 // The invalid [Arguments] row is the point of these tests
#pragma warning disable TUnit0019 // Subject is inspected through reflection discovery, not run as a test

namespace TUnit.UnitTests;

/// <summary>
/// Reflection mode reports constraint-breaking <c>[Arguments]</c> rows and <c>[GenerateGenericTest]</c> types
/// as one failed test each, in the source generator's order: rows first, then types (#7153).
/// </summary>
public class GenericConstraintViolationOrderTests
{
    [Test]
    public async Task Invalid_Rows_Are_Reported_Before_Invalid_Types()
    {
        var method = typeof(Subject).GetMethod(nameof(Subject.InvalidRowAndInvalidType), BindingFlags.Public | BindingFlags.Instance)!;

        var failedMetadata = ReflectionTestDataCollector.ResolveMethodInstantiations(typeof(Subject), method)
            .Select(x => x.FailedMetadata)
            .OfType<TestMetadata>()
            .Single();

        var violations = failedMetadata.DataSources.Cast<GenericConstraintViolationDataSource>().ToArray();

        await Assert.That(violations).Count().IsEqualTo(2);
        await Assert.That(violations[0].InnerDataSource).IsTypeOf<ArgumentsAttribute>();
        await Assert.That(violations[0].Message).Contains("Type 'String' does not satisfy the 'struct' constraint");
        await Assert.That(violations[1].InnerDataSource).IsNull();
        await Assert.That(violations[1].Message).Contains("Type 'Object' does not satisfy the 'struct' constraint");
    }

    // Not a [Test]: only inspected through reflection discovery
    public class Subject
    {
        [GenerateGenericTest(typeof(object))]
        [Arguments("x")]
        public void InvalidRowAndInvalidType<T>(T value) where T : struct
        {
        }
    }
}
