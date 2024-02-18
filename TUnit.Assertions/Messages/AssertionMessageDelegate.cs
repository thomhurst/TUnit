namespace TUnit.Assertions.Messages;

public class AssertionMessageDelegate : AssertionMessage
{
    private readonly Func<Exception?, string>? _funcValueProvider;

    public AssertionMessageDelegate(string? value) : base(value)
    {
    }

    public AssertionMessageDelegate(Func<string>? funcProvider) : base(funcProvider)
    {
    }

    public AssertionMessageDelegate(Func<Exception?, string> funcValueProvider)
    {
        _funcValueProvider = funcValueProvider;
    }

    public override string? GetValue(object? actual, Exception? exception)
    {
        return _funcValueProvider?.Invoke(exception)
            ?? base.GetValue(actual, exception);
    }
    
    public static implicit operator AssertionMessageDelegate(string value)
    {
        return new(value);
    }

    public static implicit operator AssertionMessageDelegate(Func<string> value)
    {
        return new(value);
    }
    
    public static implicit operator AssertionMessageDelegate(Func<Exception?, string> value)
    {
        return new AssertionMessageDelegate(value);
    }
}