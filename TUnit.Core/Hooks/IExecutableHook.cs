namespace TUnit.Core.Hooks;

public interface IExecutableHook<in T>
{
    string Name { get; }
    SourceGeneratedMethodInformation MethodInfo { get; }
    int Order { get; }
    Task ExecuteAsync(T context, CancellationToken cancellationToken);
}