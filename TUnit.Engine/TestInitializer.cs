using TUnit.Core;
using TUnit.Engine.Extensions;

namespace TUnit.Engine;

internal class TestInitializer
{
    public async Task InitializeTest(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        await PropertyInjectionService.InjectPropertiesIntoObjectAsync(
            test.Context.TestDetails.ClassInstance,
            test.Context.ObjectBag,
            test.Context.TestDetails.MethodMetadata,
            test.Context.Events);

        foreach (var obj in test.Context.GetEligibleEventObjects())
        {
            await ObjectInitializer.InitializeAsync(obj, cancellationToken).ConfigureAwait(false);
        }
    }
}
