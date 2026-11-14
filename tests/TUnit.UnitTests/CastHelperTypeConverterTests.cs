using System.ComponentModel;
using System.Globalization;
using TUnit.Core.Helpers;

namespace TUnit.UnitTests;

/// <summary>
/// These types are never test parameters, so no converter is generated for them and
/// CastHelper has to use its reflection fallback.
/// </summary>
public class CastHelperTypeConverterTests
{
    [Test]
    public async Task Fallback_Uses_Converter_Overriding_Only_ConvertFrom()
    {
        var result = CastHelper.Cast(typeof(ConvertFromOnly), "abc");

        await Assert.That(result).IsEqualTo(new ConvertFromOnly("abc"));
    }

    [Test]
    public async Task Fallback_Ignores_Converter_Without_ConvertFrom()
    {
        // The base TypeConverter.ConvertFrom throws, so the value is left unconverted instead.
        var result = CastHelper.Cast(typeof(NoConvertFrom), "abc");

        await Assert.That(result).IsEqualTo("abc");
    }

    [TypeConverter(typeof(ConvertFromOnlyConverter))]
    public sealed record ConvertFromOnly(string Value);

    public sealed class ConvertFromOnlyConverter : TypeConverter
    {
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
            => value is string s ? new ConvertFromOnly(s) : base.ConvertFrom(context, culture, value);
    }

    [TypeConverter(typeof(TypeConverter))]
    public sealed record NoConvertFrom(string Value);
}
