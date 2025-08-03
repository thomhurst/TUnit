namespace TUnit.Core;

/// <summary>
/// Represents test arguments that preserve type information to avoid boxing
/// </summary>
public abstract class TypedTestArguments
{
    /// <summary>
    /// Gets the typed value if it matches the expected type
    /// </summary>
    public abstract bool TryGetTypedValue<T>(out T value);
    
    /// <summary>
    /// Gets the arguments as an object array (may cause boxing)
    /// </summary>
    public abstract object?[]? GetUntypedArguments();
}

/// <summary>
/// Single-value typed test arguments
/// </summary>
public sealed class TypedTestArguments<T> : TypedTestArguments
{
    private readonly T _value;
    
    public TypedTestArguments(T value)
    {
        _value = value;
    }
    
    public override bool TryGetTypedValue<TResult>(out TResult value)
    {
        if (typeof(TResult) == typeof(T))
        {
            value = (TResult)(object)_value!;
            return true;
        }
        value = default!;
        return false;
    }
    
    public override object?[]? GetUntypedArguments()
    {
        return [_value];
    }
}

/// <summary>
/// Tuple-based typed test arguments for multiple parameters
/// </summary>
public sealed class TupleTypedTestArguments<T1, T2> : TypedTestArguments
{
    private readonly T1 _item1;
    private readonly T2 _item2;
    
    public TupleTypedTestArguments(T1 item1, T2 item2)
    {
        _item1 = item1;
        _item2 = item2;
    }
    
    public override bool TryGetTypedValue<T>(out T value)
    {
        if (typeof(T) == typeof((T1, T2)))
        {
            value = (T)(object)(_item1, _item2)!;
            return true;
        }
        value = default!;
        return false;
    }
    
    public override object?[]? GetUntypedArguments()
    {
        return [_item1, _item2];
    }
}