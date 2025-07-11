using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core.Helpers;

/// <summary>
/// AOT-compatible helper methods for data source processing
/// </summary>
public static class DataSourceHelpers
{
    /// <summary>
    /// Invokes a Func&lt;T&gt; if the value is one, otherwise returns the value as-is.
    /// This is AOT-compatible by using generic type parameters instead of reflection.
    /// </summary>
    public static object? InvokeIfFunc(object? value)
    {
        if (value == null) return null;
        
        // Try common Func<T> types without reflection
        // Note: More specific types must come before Func<object> to avoid unreachable code
        switch (value)
        {
            case Func<string> funcString:
                return funcString();
            case Func<int> funcInt:
                return funcInt();
            case Func<long> funcLong:
                return funcLong();
            case Func<double> funcDouble:
                return funcDouble();
            case Func<float> funcFloat:
                return funcFloat();
            case Func<bool> funcBool:
                return funcBool();
            case Func<decimal> funcDecimal:
                return funcDecimal();
            case Func<DateTime> funcDateTime:
                return funcDateTime();
            case Func<Guid> funcGuid:
                return funcGuid();
            case Func<object> func:
                return func();
            default:
                // For non-Func types or unsupported Func types, return as-is
                return value;
        }
    }
    
    /// <summary>
    /// Generic version that can be used when the type is known at compile time
    /// </summary>
    public static T InvokeIfFunc<T>(object? value)
    {
        if (value is Func<T> func)
        {
            return func();
        }
        return (T)value!;
    }
    
    /// <summary>
    /// Handles tuple values for method and class arguments in an AOT-compatible way
    /// </summary>
    public static Func<Task<object?>>[] HandleTupleValue(object? value, bool shouldUnwrap)
    {
        if (!shouldUnwrap || value == null)
        {
            return new[] { () => Task.FromResult<object?>(value) };
        }
        
        // Use AOT-compatible tuple unwrapping
        var unwrapped = UnwrapTupleAot(value);
        if (unwrapped.Length > 1)
        {
            // Multiple values from tuple - create a factory for each
            return unwrapped.Select(v => new Func<Task<object?>>(() => Task.FromResult<object?>(v))).ToArray();
        }
        
        // Single value or not a tuple
        return new[] { () => Task.FromResult<object?>(value) };
    }
    
    /// <summary>
    /// AOT-compatible tuple unwrapping that handles common tuple types without reflection
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2091:Target generic argument does not satisfy 'DynamicallyAccessedMembersAttribute' in target method or type.",
        Justification = "We handle specific known tuple types without reflection")]
    public static object?[] UnwrapTupleAot(object? value)
    {
        if (value == null) return new object?[] { null };
        
        // Handle common ValueTuple types explicitly
        switch (value)
        {
            case ValueTuple<object?> vt1:
                return new[] { vt1.Item1 };
            case ValueTuple<object?, object?> vt2:
                return new[] { vt2.Item1, vt2.Item2 };
            case ValueTuple<object?, object?, object?> vt3:
                return new[] { vt3.Item1, vt3.Item2, vt3.Item3 };
            case ValueTuple<object?, object?, object?, object?> vt4:
                return new[] { vt4.Item1, vt4.Item2, vt4.Item3, vt4.Item4 };
            case ValueTuple<object?, object?, object?, object?, object?> vt5:
                return new[] { vt5.Item1, vt5.Item2, vt5.Item3, vt5.Item4, vt5.Item5 };
            case ValueTuple<object?, object?, object?, object?, object?, object?> vt6:
                return new[] { vt6.Item1, vt6.Item2, vt6.Item3, vt6.Item4, vt6.Item5, vt6.Item6 };
            case ValueTuple<object?, object?, object?, object?, object?, object?, object?> vt7:
                return new[] { vt7.Item1, vt7.Item2, vt7.Item3, vt7.Item4, vt7.Item5, vt7.Item6, vt7.Item7 };
            
            // Handle regular Tuple types
            case Tuple<object?> t1:
                return new[] { t1.Item1 };
            case Tuple<object?, object?> t2:
                return new[] { t2.Item1, t2.Item2 };
            case Tuple<object?, object?, object?> t3:
                return new[] { t3.Item1, t3.Item2, t3.Item3 };
            case Tuple<object?, object?, object?, object?> t4:
                return new[] { t4.Item1, t4.Item2, t4.Item3, t4.Item4 };
            case Tuple<object?, object?, object?, object?, object?> t5:
                return new[] { t5.Item1, t5.Item2, t5.Item3, t5.Item4, t5.Item5 };
            case Tuple<object?, object?, object?, object?, object?, object?> t6:
                return new[] { t6.Item1, t6.Item2, t6.Item3, t6.Item4, t6.Item5, t6.Item6 };
            case Tuple<object?, object?, object?, object?, object?, object?, object?> t7:
                return new[] { t7.Item1, t7.Item2, t7.Item3, t7.Item4, t7.Item5, t7.Item6, t7.Item7 };
            
            default:
                // For backward compatibility, fall back to reflection-based approach if available
                // This allows existing code to continue working in non-AOT scenarios
                if (TupleHelper.IsTupleType(value.GetType()))
                {
                    return TupleHelper.UnwrapTuple(value);
                }
                
                // Not a tuple, return as single-element array
                return new object?[] { value };
        }
    }
    
    /// <summary>
    /// Generic tuple unwrapping for when types are known at compile time
    /// </summary>
    public static object?[] UnwrapTuple<T1>(ValueTuple<T1> tuple)
        => new object?[] { tuple.Item1 };
    
    public static object?[] UnwrapTuple<T1, T2>(ValueTuple<T1, T2> tuple)
        => new object?[] { tuple.Item1, tuple.Item2 };
    
    public static object?[] UnwrapTuple<T1, T2, T3>(ValueTuple<T1, T2, T3> tuple)
        => new object?[] { tuple.Item1, tuple.Item2, tuple.Item3 };
    
    public static object?[] UnwrapTuple<T1, T2, T3, T4>(ValueTuple<T1, T2, T3, T4> tuple)
        => new object?[] { tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4 };
    
    public static object?[] UnwrapTuple<T1, T2, T3, T4, T5>(ValueTuple<T1, T2, T3, T4, T5> tuple)
        => new object?[] { tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5 };
    
    public static object?[] UnwrapTuple<T1, T2, T3, T4, T5, T6>(ValueTuple<T1, T2, T3, T4, T5, T6> tuple)
        => new object?[] { tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6 };
    
    public static object?[] UnwrapTuple<T1, T2, T3, T4, T5, T6, T7>(ValueTuple<T1, T2, T3, T4, T5, T6, T7> tuple)
        => new object?[] { tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6, tuple.Item7 };
}