#nullable disable

using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Assertions.Collections;

public static partial class HasExtensions
{
    public static StringLength HasLength(this IValueSource<string> valueSource)
    {
        valueSource.AssertionBuilder.AppendCallerMethod([]);
        return new StringLength(valueSource);
    }
}