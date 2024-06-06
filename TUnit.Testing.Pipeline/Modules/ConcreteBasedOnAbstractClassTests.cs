using FluentAssertions;
using ModularPipelines.Context;

namespace TUnit.Testing.Pipeline.Modules;

public class ConcreteBasedOnAbstractClassTests : TestModule
{
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return await RunTestsWithFilter(context, 
            "/*/*/(ConcreteClass1|ConcreteClass2)/*",
            [
                result => result.Successful.Should().BeFalse(),
                result => result.Total.Should().Be(3),
                result => result.Passed.Should().Be(2),
                result => result.Failed.Should().Be(1),
                result => result.Skipped.Should().Be(0)
            ], cancellationToken);
    }
}