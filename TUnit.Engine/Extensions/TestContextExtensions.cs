using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using TUnit.Core;
using TUnit.Engine.Models;
using TUnit.Engine.Services;

namespace TUnit.Engine.Extensions;

public static class TestContextExtensions
{
    [Experimental("WIP")]
    public static async Task ReregisterTestWithArguments(
        this TestContext testContext, 
        object?[]? methodArguments, 
        Dictionary<string, object?>? objectBag = null)
    {
        var testMetadata = testContext.OriginalMetadata;

        var newTestMetaData = testMetadata.CloneWithNewMethodFactory(async (@class, token) =>
                {
                    var hasTimeout = testContext.TestDetails.Timeout != null;

                    var args = GetArgs(methodArguments, hasTimeout, token);

                    try
                    {
                        await AsyncConvert.Convert(testContext.TestDetails.MethodInfo.Invoke(@class, args));
                    }
                    catch (TargetInvocationException e)
                    {
                        ExceptionDispatchInfo.Throw(e.InnerException ?? e);
                    }
                }
            ) with
            {
                TestId = Guid.NewGuid().ToString(),
                TestMethodArguments = methodArguments ?? [],
                ObjectBag = objectBag ?? [],
            };
        
        var newTest = testContext.GetService<TestsConstructor>().ConstructTest(newTestMetaData);
        
        var startTime = DateTimeOffset.UtcNow;
        
        await testContext.GetService<TestRegistrar>().RegisterInstance(newTest,
            onFailureToInitialize: exception => testContext.GetService<ITUnitMessageBus>().Failed(newTest.TestContext, exception, startTime));
        
        _ = testContext.GetService<TestsExecutor>().ExecuteAsync(new GroupedTests
        {
            AllValidTests = [newTest],
            Parallel = new Queue<DiscoveredTest>([newTest]),
            NotInParallel = new Queue<DiscoveredTest>(),
            KeyedNotInParallel = []
        }, null, testContext.GetService<ExecuteRequestContext>());
    }

    private static object?[]? GetArgs(object?[]? methodArguments, bool hasTimeout, CancellationToken token)
    {
        if (!hasTimeout)
        {
            return methodArguments;
        }

        if (methodArguments is null)
        {
            return [token];
        }
        
        return [..methodArguments, token];
    }
}