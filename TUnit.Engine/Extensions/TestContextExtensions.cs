using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Polyfills;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Exceptions;
using TUnit.Engine.Models;
using TUnit.Engine.Services;
using AggregateException = System.AggregateException;

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

        var testBuilderContext = new TestBuilderContext();
        
        foreach (var (key, value) in objectBag ?? [])
        {
            testBuilderContext.ObjectBag.Add(key, value);
        }
        
        var newTestMetaData = testMetadata.CloneWithNewMethodFactory(async (@class, token) =>
                {
                    var hasTimeout = testContext.TestDetails.Timeout != null;

                    var args = GetArgs(methodArguments, hasTimeout, token);

                    try
                    {
                        await AsyncConvert.Convert(testContext.TestDetails.TestMethod.ReflectionInformation.Invoke(@class, args));
                    }
                    catch (TargetInvocationException e)
                    {
                        ExceptionDispatchInfo.Capture(e.InnerException ?? e).Throw();
                    }
                }
            ) with
            {
                TestId = Guid.NewGuid().ToString(),
                TestMethodArguments = methodArguments ?? [],
                TestBuilderContext = testBuilderContext
            };
        
        var newTest = testContext.GetService<TestsConstructor>().ConstructTest(newTestMetaData);
        
        var startTime = DateTimeOffset.UtcNow;
        
        await testContext.GetService<TestRegistrar>().RegisterInstance(newTest,
            onFailureToInitialize: exception => testContext.GetService<ITUnitMessageBus>().Failed(newTest.TestContext, exception, startTime));
        
        _ = testContext.GetService<TestsExecutor>().ExecuteAsync(new GroupedTests
        {
            AllValidTests = [newTest],
            Parallel = [newTest],
            NotInParallel = new PriorityQueue<DiscoveredTest, int>(),
            KeyedNotInParallel = new Dictionary<ConstraintKeysCollection, PriorityQueue<DiscoveredTest, int>>(),
            ParallelGroups = new Dictionary<string, List<DiscoveredTest>>()
        }, null, testContext.GetService<ExecuteRequestContext>());
    }
    
    internal static void SetResult(this TestContext testContext, Exception? exception)
    {
        if (exception != null && Equals(exception, testContext.Result?.Exception))
        {
            return;
        }
        
        var status = exception switch
        {
            null => Status.Passed,
            SkipTestException => Status.Skipped,
            _ => Status.Failed,
        };

        if (testContext.Result?.Exception is not null && exception is not null)
        {
            exception = new AggregateException(testContext.Result.Exception, exception);
        }

        var start = testContext.Timings.MinBy(x => x.Start)?.Start;
        var end = testContext.Timings.MaxBy(x => x.End)?.End;
        
        testContext.Result = new TestResult
        {
            TestContext = testContext,
            Duration = end - start,
            Start = start,
            End = end,
            ComputerName = Environment.MachineName,
            Exception = exception,
            Status = status,
            Output = $"{testContext.GetErrorOutput()}{Environment.NewLine}{testContext.GetStandardOutput()}".Trim()
        };
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