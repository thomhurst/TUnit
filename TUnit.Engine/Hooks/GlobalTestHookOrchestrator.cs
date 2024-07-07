using System.Runtime.CompilerServices;
using TUnit.Core;
using TUnit.Engine.Extensions;

namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public static class GlobalTestHookOrchestrator
{
    private static readonly List<Func<TestContext, Task>> SetUps = [];
    private static readonly List<Func<TestContext, Task>> CleanUps = [];

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static void RegisterSetUp(StaticMethod<TestContext> staticMethod)
    {
        SetUps.Add(context =>
        {
            var cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(EngineCancellationToken.CancellationTokenSource.Token);
            var timeout = staticMethod.MethodInfo.GetTimeout();

            if (timeout != null)
            {
                cancellationToken.CancelAfter(timeout.Value);
            }
            
            return staticMethod.Body(context, cancellationToken.Token);
        });
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static void RegisterCleanUp(StaticMethod<TestContext> staticMethod)
    {
        CleanUps.Add(context =>
        {
            var cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(EngineCancellationToken.CancellationTokenSource.Token);
            var timeout = staticMethod.MethodInfo.GetTimeout();

            if (timeout != null)
            {
                cancellationToken.CancelAfter(timeout.Value);
            }

            return staticMethod.Body(context, cancellationToken.Token);
        });
    }

    public static async Task ExecuteSetups(TestContext testContext, CancellationToken token)
    {
        foreach (var setUp in SetUps)
        {
            await setUp(testContext);
        }
    }

    public static async Task ExecuteCleanUps(TestContext testContext, List<Exception> cleanUpExceptions,
        CancellationToken token)
    {
        foreach (var cleanUp in CleanUps)
        {
            try
            {
                await cleanUp(testContext);
            }
            catch (Exception e)
            {
                cleanUpExceptions.Add(e);
            }
        }

        if (cleanUpExceptions.Count == 1)
        {
            throw cleanUpExceptions[0];
        }

        if (cleanUpExceptions.Count > 1)
        {
            throw new AggregateException(cleanUpExceptions);
        }
    }
}