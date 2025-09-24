using System.Globalization;

namespace TUnit.Core.Helpers;

/// <summary>
/// Helper methods for parsing decimal values with culture fallback support
/// </summary>
public static class DecimalParsingHelper
{
    /// <summary>
    /// Tries to parse a decimal value from a string, first using the current culture,
    /// then falling back to the invariant culture if that fails.
    /// This is useful for handling decimal values in attributes that might be written
    /// with different decimal separators (e.g., "123.456" vs "123,456").
    /// </summary>
    public static decimal ParseDecimalWithCultureFallback(string value)
    {
        // First, try parsing with the current culture
        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out var result))
        {
            return result;
        }
        
        // If that fails, try with the invariant culture
        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
        {
            return result;
        }
        
        // If both fail, throw an exception with helpful details
        throw new FormatException(
            $"Could not parse '{value}' as a decimal value. " +
            $"Tried both CurrentCulture ({CultureInfo.CurrentCulture.Name}) " +
            $"and InvariantCulture. " +
            $"Valid decimal formats include: 123.456 (invariant) or locale-specific format."
        );
    }
}