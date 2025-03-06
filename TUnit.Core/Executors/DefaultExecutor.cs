namespace TUnit.Core;

public class DefaultExecutor : GenericAbstractExecutor
{
    public static readonly DefaultExecutor Instance = new();
    
    private DefaultExecutor()
    {
    }

    protected override ValueTask ExecuteAsync(Func<ValueTask> action)
    {
        return action();
    }
}