#nullable disable

using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class HasExtensions
{
    public static StringLength<TAnd, TOr> HasLength<TAnd, TOr>(this IValueSource<string, TAnd, TOr> valueSource)
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new StringLength<TAnd, TOr>(valueSource.AssertionBuilder.AppendCallerMethod(null));
    }
}