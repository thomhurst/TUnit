namespace TUnit.Core.SourceGenerator.Extensions;

public static class ObjectExtensions
{
    public static string? ToInvariantString(this object? obj)
    {
        if(obj is IFormattable formattable)
        {
            return formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture);
        }

        return obj?.ToString();
    }
}
