namespace TUnit.Assertions.Messages;

public class AssertionMessageValue<TActual> : AssertionMessage
{
    private readonly Func<TActual?, string>? _funcValueProvider;

    public AssertionMessageValue(string? value) : base(value)
    {
    }

    public AssertionMessageValue(Func<string>? funcProvider) : base(funcProvider)
    {
    }

    public AssertionMessageValue(Func<TActual?, string> funcValueProvider)
    {
        _funcValueProvider = funcValueProvider;
    }
    
    public override string? GetValue(object? actual, Exception? exception)
    {
        return _funcValueProvider?.Invoke(actual is TActual castActual ? castActual : default)
               ?? base.GetValue(actual, exception);
    }
    
    public static implicit operator AssertionMessageValue<TActual>(string value)
    {
        return new(value);
    }

    public static implicit operator AssertionMessageValue<TActual>(Func<string> value)
    {
        return new(value);
    }
    
    public static implicit operator AssertionMessageValue<TActual>(Func<TActual?, string> value)
    {
        return new AssertionMessageValue<TActual>(value);
    }
}