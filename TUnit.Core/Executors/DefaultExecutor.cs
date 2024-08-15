using System.Runtime.CompilerServices;

namespace TUnit.Core;

public class DefaultExecutor : GenericAbstractExecutor
{
    public static readonly DefaultExecutor Instance = new();
    
    private DefaultExecutor()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override Task ExecuteAsync(Func<Task> action)
    {
        return action();
    }
}