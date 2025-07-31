using System.Collections.Concurrent;

namespace TUnit.Core.Converters;

/// <summary>
/// Registry for AOT-compatible type converters
/// </summary>
public static class AotConverterRegistry
{
    private static readonly ConcurrentDictionary<(Type Source, Type Target), IAotConverter> Converters = new();
    
    /// <summary>
    /// Registers a converter
    /// </summary>
    public static void Register(IAotConverter converter)
    {
        Converters.TryAdd((converter.SourceType, converter.TargetType), converter);
    }
    
    /// <summary>
    /// Tries to get a converter for the specified types
    /// </summary>
    public static bool TryGetConverter(Type sourceType, Type targetType, out IAotConverter? converter)
    {
        return Converters.TryGetValue((sourceType, targetType), out converter);
    }
    
    /// <summary>
    /// Tries to convert a value using a registered converter
    /// </summary>
    public static bool TryConvert(Type sourceType, Type targetType, object? value, out object? result)
    {
        if (TryGetConverter(sourceType, targetType, out var converter) && converter != null)
        {
            result = converter.Convert(value);
            return true;
        }
        
        result = null;
        return false;
    }
}