#nullable disable

using TUnit.Assertions.AssertConditions.Interfaces;

namespace TUnit.Assertions.Extensions;

public static partial class HasExtensions
{
    public static StringLength HasLength(this IValueSource<string> valueSource)
    {
        valueSource.AssertionBuilder.AppendCallerMethod([]);
        return new StringLength(valueSource);
    }
}