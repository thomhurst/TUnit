namespace TUnit.Logging.Microsoft;

/// <summary>
/// Manages logging scope state using AsyncLocal for proper async flow.
/// </summary>
internal sealed class TUnitLoggerScope : IDisposable
{
    private static readonly AsyncLocal<TUnitLoggerScope?> CurrentScope = new();

    private readonly object _state;
    private readonly TUnitLoggerScope? _parent;
    private bool _disposed;

    private TUnitLoggerScope(object state, TUnitLoggerScope? parent)
    {
        _state = state;
        _parent = parent;
    }

    public static TUnitLoggerScope? Current => CurrentScope.Value;

    public static IDisposable Push(object state)
    {
        var scope = new TUnitLoggerScope(state, CurrentScope.Value);
        CurrentScope.Value = scope;
        return scope;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        CurrentScope.Value = _parent;
    }

    public override string ToString()
    {
        var current = this;
        var scopes = new List<string>();

        while (current != null)
        {
            scopes.Add(current._state.ToString() ?? string.Empty);
            current = current._parent;
        }

        scopes.Reverse();
        return string.Join(" => ", scopes);
    }
}
