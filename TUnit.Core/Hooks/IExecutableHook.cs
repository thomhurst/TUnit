namespace TUnit.Core.Hooks;

public interface IExecutableHook<in T>
{
    string Name { get; }
    SourceGeneratedMethodInformation MethodInfo { get; }
    int Order { get; }
    ValueTask ExecuteAsync(T context, CancellationToken cancellationToken);
}