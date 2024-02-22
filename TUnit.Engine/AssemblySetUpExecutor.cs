using System.Collections.Concurrent;
using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine;

internal class AssemblySetUpExecutor
{
    private readonly MethodInvoker _methodInvoker;
    private readonly ConcurrentDictionary<CachedAssemblyInformation, Task> _tasks = new();

    public AssemblySetUpExecutor(MethodInvoker methodInvoker)
    {
        _methodInvoker = methodInvoker;
    }
    
    public async Task ExecuteSetUps(CachedAssemblyInformation cachedAssemblyInformation)
    {
        await _tasks.GetOrAdd(cachedAssemblyInformation, ExecuteCore);
    }

    private async Task ExecuteCore(CachedAssemblyInformation cachedAssemblyInformation)
    {
        foreach (var methodInfo in cachedAssemblyInformation.Methods
                     .Where(x => x.GetCustomAttribute<AssemblySetUpAttribute>() != null))
        {
            await _methodInvoker.InvokeMethod(null, methodInfo, BindingFlags.Static | BindingFlags.Public, null,
                CancellationToken.None);
        }
    }
}