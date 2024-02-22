using System.Collections.Concurrent;
using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine;

internal class AssemblyCleanUpExecutor
{
    private readonly MethodInvoker _methodInvoker;
    private readonly ConcurrentDictionary<CachedAssemblyInformation, Task> _tasks = new();

    public AssemblyCleanUpExecutor(MethodInvoker methodInvoker)
    {
        _methodInvoker = methodInvoker;
    }
    
    public async Task ExecuteCleanUps(CachedAssemblyInformation cachedAssemblyInformation)
    {
        await _tasks.GetOrAdd(cachedAssemblyInformation, ExecuteCore);
    }

    private async Task ExecuteCore(CachedAssemblyInformation cachedAssemblyInformation)
    {
        foreach (var methodInfo in cachedAssemblyInformation.Methods
                     .Where(x => x.GetCustomAttribute<AssemblyCleanUpAttribute>() != null))
        {
            await _methodInvoker.InvokeMethod(null, methodInfo, BindingFlags.Static | BindingFlags.Public, null,
                CancellationToken.None);
        }
    }
}