#nullable disable

using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class HasExtensions
{
    public static StringLength<TAnd, TOr> HasLength<TAnd, TOr>(this IHas<string, TAnd, TOr> has)
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return new StringLength<TAnd, TOr>(has.AssertionConnector.AssertionBuilder.AppendCallerMethod(null), has.AssertionConnector.ChainType, has.AssertionConnector.OtherAssertCondition);
    }
}