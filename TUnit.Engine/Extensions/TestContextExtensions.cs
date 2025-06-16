using System.Collections.Concurrent;
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

#pragma warning disable

namespace TUnit.Engine.Extensions;

public static class TestContextExtensions
{
    [RequiresUnreferencedCode("Reflection")]
    [Experimental("WIP")]
    public static async Task ReregisterTestWithArguments(
        this TestContext testContext, 
        object?[]? methodArguments, 
        Dictionary<string, object?>? objectBag = null)
    {
        // TODO: Rework to use DynamicTestRegistrar
        
        var testMetadata = testContext.OriginalMetadata;

        var testBuilderContext = new TestBuilderContext
        {
            TestMethodName = testContext.TestDetails.TestMethod.Name,
            ClassInformation = testContext.TestDetails.TestClass,
            MethodInformation = testContext.TestDetails.TestMethod
        };
        
        foreach (var (key, value) in objectBag ?? [])
        {
            testBuilderContext.ObjectBag.Add(key, value);
        }
        
        foreach (var dataAttribute in testContext.OriginalMetadata.TestBuilderContext.DataAttributes)
        {
            testBuilderContext.DataAttributes.Add(dataAttribute);
        }
        
        var newTestMetaData = testMetadata.CloneWithNewMethodFactory(async (@class, token) =>
                {
                    var hasTimeout = testContext.TestDetails.Timeout != null;

                    var args = GetArgs(methodArguments, hasTimeout, token);

                    try
                    {
                        await AsyncConvert.ConvertObject(testMetadata.TestMethod.ReflectionInformation.Invoke(@class, args));
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
        
        var newTest = testContext.GetService<UnifiedTestBuilder>().BuildTest(newTestMetaData);
        
        var startTime = DateTimeOffset.UtcNow;

        await testContext.GetService<TUnitMessageBus>().Discovered(newTest.TestContext);
        
        await testContext.GetService<TestRegistrar>().RegisterInstance(newTest,
            onFailureToInitialize: exception => testContext.GetService<ITUnitMessageBus>().Failed(newTest.TestContext, exception, startTime));
        
        _ = testContext.GetService<TestsExecutor>().ExecuteAsync(new GroupedTests
        {
            AllValidTests = [newTest],
            Parallel = [newTest],
            NotInParallel = new PriorityQueue<DiscoveredTest, int>(),
            KeyedNotInParallel = new Dictionary<ConstraintKeysCollection, PriorityQueue<DiscoveredTest, int>>(),
            ParallelGroups = new ConcurrentDictionary<ParallelGroupConstraint, List<DiscoveredTest>>()
        }, null, testContext.GetService<EngineCancellationToken>().CancellationTokenSource.Token);
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
            TestRunCanceledException => Status.Cancelled,
            TaskCanceledException or OperationCanceledException 
                when testContext.GetService<EngineCancellationToken>().Token.IsCancellationRequested => Status.Cancelled,
            _ => Status.Failed,
        };

        if (testContext.Result?.Exception is not null && exception is not null)
        {
            if (exception is AggregateException aggregateException)
            {
                exception = new AggregateException([
                    testContext.Result.Exception, ..aggregateException.InnerExceptions
                ]);
            }
            else
            {
                exception = new AggregateException(testContext.Result.Exception, exception);
            }
        }

        DateTimeOffset? now = null;
        
        var start = testContext.Timings.MinBy(x => x.Start)?.Start ?? (now ??= DateTimeOffset.UtcNow);
        var end = testContext.Timings.MaxBy(x => x.End)?.End ?? (now ??= DateTimeOffset.UtcNow);
        
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