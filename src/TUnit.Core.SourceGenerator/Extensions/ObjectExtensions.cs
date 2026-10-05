namespace TUnit.Core.SourceGenerator.Extensions;

public static class ObjectExtensions
{
    /// <summary>
    /// Formats a value the same way under every culture: numbers, dates and other <see cref="IFormattable"/>
    /// values use <see cref="System.Globalization.CultureInfo.InvariantCulture"/>. Anything else (strings,
    /// bools, chars, Roslyn symbols) falls back to <c>ToString()</c>, which does not depend on
    /// the culture for the values generators see, such as <c>TypedConstant.Value</c>. Returns null for null.
    /// </summary>
    public static string? ToInvariantString(this object? obj)
    {
        if(obj is IFormattable formattable)
        {
            return formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture);
        }

        return obj?.ToString();
    }
}
