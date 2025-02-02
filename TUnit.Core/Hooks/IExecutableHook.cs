using System.Reflection;

namespace TUnit.Core.Hooks;

public interface IExecutableHook<in T>
{
    string Name { get; }
    SourceGeneratedMethodInformation MethodInfo { get; }
    int Order { get; }
    bool Execute(T context, CancellationToken cancellationToken);
    Task ExecuteAsync(T context, CancellationToken cancellationToken);
    bool IsSynchronous { get; }
}