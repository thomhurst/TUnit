namespace TUnit.Core.Converters;

/// <summary>
/// Interface for AOT-compatible type converters
/// </summary>
public interface IAotConverter
{
    /// <summary>
    /// The source type this converter can convert from
    /// </summary>
    Type SourceType { get; }
    
    /// <summary>
    /// The target type this converter can convert to
    /// </summary>
    Type TargetType { get; }
    
    /// <summary>
    /// Performs the conversion
    /// </summary>
    /// <param name="value">The value to convert</param>
    /// <returns>The converted value</returns>
    object? Convert(object? value);
}