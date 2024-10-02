namespace TUnit.Core;

public class DefaultExecutor : GenericAbstractExecutor
{
    public static readonly DefaultExecutor Instance = new();
    
    private DefaultExecutor()
    {
    }

    protected override Task ExecuteAsync(Func<Task> action)
    {
        return action();
    }
}