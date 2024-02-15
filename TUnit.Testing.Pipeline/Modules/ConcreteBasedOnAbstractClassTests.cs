using ModularPipelines.Context;
using ModularPipelines.Models;

namespace TUnit.Testing.Pipeline;

public class ConcreteBasedOnAbstractClassTests : TestModule
{
    protected override async Task<CommandResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return await RunTestsWithFilter(context, "TestClass=ConcreteClass1,ConcreteClass2");
    }
}