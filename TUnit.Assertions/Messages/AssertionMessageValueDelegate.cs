namespace TUnit.Assertions.Messages;

public class AssertionMessageValueDelegate<TActual> : AssertionMessage
{
    private readonly Func<TActual?, Exception?, string>? _funcValueProvider;

    public AssertionMessageValueDelegate(string? value) : base(value)
    {
    }

    public AssertionMessageValueDelegate(Func<string>? funcProvider) : base(funcProvider)
    {
    }

    public AssertionMessageValueDelegate(Func<TActual?, Exception?, string> funcValueProvider)
    {
        _funcValueProvider = funcValueProvider;
    }
    
    public override string? GetValue(object? actual, Exception? exception)
    {
        return _funcValueProvider?.Invoke(actual is TActual castActual ? castActual : default, exception)
               ?? base.GetValue(actual, exception);
    }
    
    public static implicit operator AssertionMessageValueDelegate<TActual>(string value)
    {
        return new(value);
    }

    public static implicit operator AssertionMessageValueDelegate<TActual>(Func<string> value)
    {
        return new(value);
    }
    
    public static implicit operator AssertionMessageValueDelegate<TActual>(Func<TActual?, Exception?, string> value)
    {
        return new AssertionMessageValueDelegate<TActual>(value);
    }
}