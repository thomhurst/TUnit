using System.ComponentModel;
using System.Globalization;

namespace TUnit.TestProject;

public class TypeConverterArgumentsTests
{
    [Test]
    [Arguments("info@example.com")]
    public async Task Converter_From_String(Email email)
    {
        await Assert.That(email.Value).IsEqualTo("info@example.com");
    }

    [Test]
    [Arguments("info@example.com")]
    [Arguments(null)]
    public async Task Nullable_Converter_From_String(Code? code)
    {
        if (code is not null)
        {
            await Assert.That(code.Value.Value).IsEqualTo("info@example.com");
        }
        else
        {
            await Assert.That(code).IsNull();
        }
    }

    [Test]
    [Arguments("abc")]
    public async Task Converter_Receives_Target_Type(TypedName name)
    {
        await Assert.That(name.Value).IsEqualTo($"{nameof(TypedName)}:abc");
    }

    [Test]
    [Arguments("abc")]
    public async Task Converter_From_Type_Name(NamedByString value)
    {
        await Assert.That(value.Value).IsEqualTo("abc");
    }

    [Test]
    [Arguments("abc")]
    public async Task Converter_Declared_On_Base_Type(DerivedName value)
    {
        await Assert.That(value.Value).IsEqualTo("derived:abc");
    }

    [TypeConverter(typeof(EmailConverter))]
    public sealed record Email(string Value);

    public sealed class EmailConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
            => value is string s ? new Email(s) : base.ConvertFrom(context, culture, value);
    }

    [TypeConverter(typeof(CodeConverter))]
    public readonly record struct Code(string Value);

    public sealed class CodeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
            => value is string s ? new Code(s) : base.ConvertFrom(context, culture, value);
    }

    [TypeConverter(typeof(TypedNameConverter))]
    public sealed record TypedName(string Value);

    public sealed class TypedNameConverter(Type type) : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
            => value is string s ? new TypedName($"{type.Name}:{s}") : base.ConvertFrom(context, culture, value);
    }

    [TypeConverter("TUnit.TestProject.TypeConverterArgumentsTests+NamedByStringConverter, TUnit.TestProject")]
    public sealed record NamedByString(string Value);

    public sealed class NamedByStringConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
            => value is string s ? new NamedByString(s) : base.ConvertFrom(context, culture, value);
    }

    [TypeConverter(typeof(BaseNameConverter))]
    public class BaseName(string value)
    {
        public string Value { get; } = value;
    }

    public sealed class DerivedName(string value) : BaseName(value);

    public sealed class BaseNameConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
            => value is string s ? new DerivedName($"derived:{s}") : base.ConvertFrom(context, culture, value);
    }
}

[Arguments("info@example.com")]
public class TypeConverterClassArgumentsTests(TypeConverterArgumentsTests.Email email)
{
    [Test]
    public async Task Class_Argument_Converted()
    {
        await Assert.That(email.Value).IsEqualTo("info@example.com");
    }
}
