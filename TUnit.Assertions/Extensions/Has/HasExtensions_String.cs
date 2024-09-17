#nullable disable

using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class HasExtensions
{
    public static StringLength<TAnd, TOr> HasLength<TAnd, TOr>(this AssertionBuilder<string, TAnd, TOr> assertionBuilder)
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new StringLength<TAnd, TOr>(assertionBuilder.AppendCallerMethod(null));
    }
}