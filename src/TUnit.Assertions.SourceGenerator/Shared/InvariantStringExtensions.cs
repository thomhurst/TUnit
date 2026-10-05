using System.Globalization;

namespace TUnit.SourceGen.Shared;

internal static class InvariantStringExtensions
{
    /// <summary>
    /// Text for an attribute argument (<c>TypedConstant.Value</c>) that does not depend on the
    /// build machine's culture; <c>null</c> stays <c>null</c>, like <c>value?.ToString()</c>.
    /// </summary>
    public static string? ToInvariantString(this object? value)
        => value is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture)
            : value?.ToString();
}
