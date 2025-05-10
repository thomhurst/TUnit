using System.Diagnostics.CodeAnalysis;
using TUnit.Core;

namespace TUnit.Engine.Services;

internal class DynamicTestRegistrar(
    BaseTestsConstructor testsConstructor,
    TestRegistrar testRegistrar,
    TestGrouper testGrouper,
    ITUnitMessageBus messageBus,
    TestsExecutor testsExecutor,
    EngineCancellationToken engineCancellationToken
    ) : IDynamicTestRegistrar
{
    public async Task Register<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors 
                                        | DynamicallyAccessedMemberTypes.PublicMethods
                                        | DynamicallyAccessedMemberTypes.PublicProperties)]
        TClass>(DynamicTest<TClass> dynamicTest) where TClass : class
    {
        var newTests = testsConstructor.ConstructTests(dynamicTest).ToArray();

        var grouped = testGrouper.OrganiseTests(newTests);
        
        var startTime = DateTimeOffset.UtcNow;

        foreach (var test in grouped.AllValidTests)
        {
            await testRegistrar.RegisterInstance(test, onFailureToInitialize: exception =>
                messageBus.Failed(test.TestContext, exception, startTime));
            
            await messageBus.Discovered(test.TestContext);
        }

        _ = testsExecutor.ExecuteAsync(grouped, null, engineCancellationToken.Token);
    }
}