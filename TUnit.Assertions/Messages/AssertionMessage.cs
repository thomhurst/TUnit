namespace TUnit.Assertions.Messages;

public class AssertionMessage
{
    private readonly Func<string>? _funcProvider;
    private readonly string? _value;

    public AssertionMessage()
    {
    }
    
    public AssertionMessage(string? value)
    {
        _value = value;
    }
    
    public AssertionMessage(Func<string>? funcProvider)
    {
        _funcProvider = funcProvider;
    }

    public virtual string? GetValue(object? actual, Exception? exception)
    {
        return _value ?? _funcProvider?.Invoke();
    }

    public static implicit operator AssertionMessage(string value)
    {
        return new AssertionMessage(value);
    }

    public static implicit operator AssertionMessage(Func<string> value)
    {
        return new AssertionMessage(value);
    }
}