using System;
using System.Runtime.CompilerServices;

namespace TUnit.Core.Helpers;

/// <summary>
/// Runtime helper for handling constructor arguments that may come as a tuple or individual values
/// </summary>
public static class ConstructorArgumentsHelper
{
    /// <summary>
    /// Extracts constructor arguments from an object array that may contain either:
    /// - A single tuple with all arguments
    /// - Individual arguments
    /// </summary>
    public static (T1, T2) ExtractArguments<T1, T2>(object?[] args)
    {
        if (args.Length == 1 && args[0] is ITuple tuple && tuple.Length == 2)
        {
            return (CastHelper.Cast<T1>(tuple[0]), CastHelper.Cast<T2>(tuple[1]));
        }
        
        if (args.Length >= 2)
        {
            return (CastHelper.Cast<T1>(args[0]), CastHelper.Cast<T2>(args[1]));
        }
        
        throw new ArgumentException($"Expected either a tuple with 2 elements or an array with at least 2 elements, but got array with {args.Length} elements");
    }
    
    /// <summary>
    /// Extracts constructor arguments from an object array that may contain either:
    /// - A single tuple with all arguments
    /// - Individual arguments
    /// </summary>
    public static (T1, T2, T3) ExtractArguments<T1, T2, T3>(object?[] args)
    {
        if (args.Length == 1 && args[0] is ITuple tuple && tuple.Length == 3)
        {
            return (CastHelper.Cast<T1>(tuple[0]), CastHelper.Cast<T2>(tuple[1]), CastHelper.Cast<T3>(tuple[2]));
        }
        
        if (args.Length >= 3)
        {
            return (CastHelper.Cast<T1>(args[0]), CastHelper.Cast<T2>(args[1]), CastHelper.Cast<T3>(args[2]));
        }
        
        throw new ArgumentException($"Expected either a tuple with 3 elements or an array with at least 3 elements, but got array with {args.Length} elements");
    }
    
    /// <summary>
    /// Extracts constructor arguments from an object array that may contain either:
    /// - A single tuple with all arguments
    /// - Individual arguments
    /// </summary>
    public static (T1, T2, T3, T4) ExtractArguments<T1, T2, T3, T4>(object?[] args)
    {
        if (args.Length == 1 && args[0] is ITuple tuple && tuple.Length == 4)
        {
            return (CastHelper.Cast<T1>(tuple[0]), CastHelper.Cast<T2>(tuple[1]), 
                    CastHelper.Cast<T3>(tuple[2]), CastHelper.Cast<T4>(tuple[3]));
        }
        
        if (args.Length >= 4)
        {
            return (CastHelper.Cast<T1>(args[0]), CastHelper.Cast<T2>(args[1]), 
                    CastHelper.Cast<T3>(args[2]), CastHelper.Cast<T4>(args[3]));
        }
        
        throw new ArgumentException($"Expected either a tuple with 4 elements or an array with at least 4 elements, but got array with {args.Length} elements");
    }
    
    /// <summary>
    /// Extracts constructor arguments from an object array that may contain either:
    /// - A single tuple with all arguments
    /// - Individual arguments
    /// </summary>
    public static (T1, T2, T3, T4, T5) ExtractArguments<T1, T2, T3, T4, T5>(object?[] args)
    {
        if (args.Length == 1 && args[0] is ITuple tuple && tuple.Length == 5)
        {
            return (CastHelper.Cast<T1>(tuple[0]), CastHelper.Cast<T2>(tuple[1]), 
                    CastHelper.Cast<T3>(tuple[2]), CastHelper.Cast<T4>(tuple[3]), 
                    CastHelper.Cast<T5>(tuple[4]));
        }
        
        if (args.Length >= 5)
        {
            return (CastHelper.Cast<T1>(args[0]), CastHelper.Cast<T2>(args[1]), 
                    CastHelper.Cast<T3>(args[2]), CastHelper.Cast<T4>(args[3]), 
                    CastHelper.Cast<T5>(args[4]));
        }
        
        throw new ArgumentException($"Expected either a tuple with 5 elements or an array with at least 5 elements, but got array with {args.Length} elements");
    }
    
    /// <summary>
    /// Extracts constructor arguments from an object array that may contain either:
    /// - A single tuple with all arguments
    /// - Individual arguments
    /// </summary>
    public static (T1, T2, T3, T4, T5, T6) ExtractArguments<T1, T2, T3, T4, T5, T6>(object?[] args)
    {
        if (args.Length == 1 && args[0] is ITuple tuple && tuple.Length == 6)
        {
            return (CastHelper.Cast<T1>(tuple[0]), CastHelper.Cast<T2>(tuple[1]), 
                    CastHelper.Cast<T3>(tuple[2]), CastHelper.Cast<T4>(tuple[3]), 
                    CastHelper.Cast<T5>(tuple[4]), CastHelper.Cast<T6>(tuple[5]));
        }
        
        if (args.Length >= 6)
        {
            return (CastHelper.Cast<T1>(args[0]), CastHelper.Cast<T2>(args[1]), 
                    CastHelper.Cast<T3>(args[2]), CastHelper.Cast<T4>(args[3]), 
                    CastHelper.Cast<T5>(args[4]), CastHelper.Cast<T6>(args[5]));
        }
        
        throw new ArgumentException($"Expected either a tuple with 6 elements or an array with at least 6 elements, but got array with {args.Length} elements");
    }
    
    /// <summary>
    /// Extracts constructor arguments from an object array that may contain either:
    /// - A single tuple with all arguments
    /// - Individual arguments
    /// </summary>
    public static (T1, T2, T3, T4, T5, T6, T7) ExtractArguments<T1, T2, T3, T4, T5, T6, T7>(object?[] args)
    {
        if (args.Length == 1 && args[0] is ITuple tuple && tuple.Length == 7)
        {
            return (CastHelper.Cast<T1>(tuple[0]), CastHelper.Cast<T2>(tuple[1]), 
                    CastHelper.Cast<T3>(tuple[2]), CastHelper.Cast<T4>(tuple[3]), 
                    CastHelper.Cast<T5>(tuple[4]), CastHelper.Cast<T6>(tuple[5]),
                    CastHelper.Cast<T7>(tuple[6]));
        }
        
        if (args.Length >= 7)
        {
            return (CastHelper.Cast<T1>(args[0]), CastHelper.Cast<T2>(args[1]), 
                    CastHelper.Cast<T3>(args[2]), CastHelper.Cast<T4>(args[3]), 
                    CastHelper.Cast<T5>(args[4]), CastHelper.Cast<T6>(args[5]),
                    CastHelper.Cast<T7>(args[6]));
        }
        
        throw new ArgumentException($"Expected either a tuple with 7 elements or an array with at least 7 elements, but got array with {args.Length} elements");
    }
    
    /// <summary>
    /// Extracts constructor arguments from an object array that may contain either:
    /// - A single tuple with all arguments
    /// - Individual arguments
    /// </summary>
    public static (T1, T2, T3, T4, T5, T6, T7, T8) ExtractArguments<T1, T2, T3, T4, T5, T6, T7, T8>(object?[] args)
    {
        if (args.Length == 1 && args[0] is ITuple tuple && tuple.Length == 8)
        {
            return (CastHelper.Cast<T1>(tuple[0]), CastHelper.Cast<T2>(tuple[1]), 
                    CastHelper.Cast<T3>(tuple[2]), CastHelper.Cast<T4>(tuple[3]), 
                    CastHelper.Cast<T5>(tuple[4]), CastHelper.Cast<T6>(tuple[5]),
                    CastHelper.Cast<T7>(tuple[6]), CastHelper.Cast<T8>(tuple[7]));
        }
        
        if (args.Length >= 8)
        {
            return (CastHelper.Cast<T1>(args[0]), CastHelper.Cast<T2>(args[1]), 
                    CastHelper.Cast<T3>(args[2]), CastHelper.Cast<T4>(args[3]), 
                    CastHelper.Cast<T5>(args[4]), CastHelper.Cast<T6>(args[5]),
                    CastHelper.Cast<T7>(args[6]), CastHelper.Cast<T8>(args[7]));
        }
        
        throw new ArgumentException($"Expected either a tuple with 8 elements or an array with at least 8 elements, but got array with {args.Length} elements");
    }
    
    /// <summary>
    /// Extracts constructor arguments from an object array that may contain either:
    /// - A single tuple with all arguments
    /// - Individual arguments
    /// </summary>
    public static (T1, T2, T3, T4, T5, T6, T7, T8, T9) ExtractArguments<T1, T2, T3, T4, T5, T6, T7, T8, T9>(object?[] args)
    {
        if (args.Length == 1 && args[0] is ITuple tuple && tuple.Length == 9)
        {
            return (CastHelper.Cast<T1>(tuple[0]), CastHelper.Cast<T2>(tuple[1]), 
                    CastHelper.Cast<T3>(tuple[2]), CastHelper.Cast<T4>(tuple[3]), 
                    CastHelper.Cast<T5>(tuple[4]), CastHelper.Cast<T6>(tuple[5]),
                    CastHelper.Cast<T7>(tuple[6]), CastHelper.Cast<T8>(tuple[7]),
                    CastHelper.Cast<T9>(tuple[8]));
        }
        
        if (args.Length >= 9)
        {
            return (CastHelper.Cast<T1>(args[0]), CastHelper.Cast<T2>(args[1]), 
                    CastHelper.Cast<T3>(args[2]), CastHelper.Cast<T4>(args[3]), 
                    CastHelper.Cast<T5>(args[4]), CastHelper.Cast<T6>(args[5]),
                    CastHelper.Cast<T7>(args[6]), CastHelper.Cast<T8>(args[7]),
                    CastHelper.Cast<T9>(args[8]));
        }
        
        throw new ArgumentException($"Expected either a tuple with 9 elements or an array with at least 9 elements, but got array with {args.Length} elements");
    }
    
    /// <summary>
    /// Extracts constructor arguments from an object array that may contain either:
    /// - A single tuple with all arguments
    /// - Individual arguments
    /// </summary>
    public static (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10) ExtractArguments<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(object?[] args)
    {
        if (args.Length == 1 && args[0] is ITuple tuple && tuple.Length == 10)
        {
            return (CastHelper.Cast<T1>(tuple[0]), CastHelper.Cast<T2>(tuple[1]), 
                    CastHelper.Cast<T3>(tuple[2]), CastHelper.Cast<T4>(tuple[3]), 
                    CastHelper.Cast<T5>(tuple[4]), CastHelper.Cast<T6>(tuple[5]),
                    CastHelper.Cast<T7>(tuple[6]), CastHelper.Cast<T8>(tuple[7]),
                    CastHelper.Cast<T9>(tuple[8]), CastHelper.Cast<T10>(tuple[9]));
        }
        
        if (args.Length >= 10)
        {
            return (CastHelper.Cast<T1>(args[0]), CastHelper.Cast<T2>(args[1]), 
                    CastHelper.Cast<T3>(args[2]), CastHelper.Cast<T4>(args[3]), 
                    CastHelper.Cast<T5>(args[4]), CastHelper.Cast<T6>(args[5]),
                    CastHelper.Cast<T7>(args[6]), CastHelper.Cast<T8>(args[7]),
                    CastHelper.Cast<T9>(args[8]), CastHelper.Cast<T10>(args[9]));
        }
        
        throw new ArgumentException($"Expected either a tuple with 10 elements or an array with at least 10 elements, but got array with {args.Length} elements");
    }
}