using System.Collections.Concurrent;

namespace TUnit.Core.Converters;

/// <summary>
/// Registry for AOT-compatible type converters
/// </summary>
public static class AotConverterRegistry
{
    private static readonly ConcurrentDictionary<(Type Source, Type Target), IAotConverter> Converters = new();

    /// <summary>
    /// Registers a converter. Called by generated code from a static field initializer on the
    /// consolidated registration <c>.cctor</c> (so the per-assembly module initializer collapses
    /// into one merged <c>.cctor</c>).
    /// Returns a dummy value for use as a static field initializer.
    /// </summary>
    public static int Register(IAotConverter converter)
    {
        Converters.TryAdd((converter.SourceType, converter.TargetType), converter);
        return 0;
    }

    /// <summary>
    /// Registers a converter
    /// </summary>
    public static void Register<TSource, TTarget>(Func<TSource, TTarget> converter)
    {
        Converters.TryAdd((typeof(TSource), typeof(TTarget)), new FuncAotConverter<TSource, TTarget>(converter));
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
