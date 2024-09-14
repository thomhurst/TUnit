#nullable disable

using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class HasExtensions
{
    public static StringLength<TAnd, TOr> Length<TAnd, TOr>(this Has<string, TAnd, TOr> has)
        where TAnd : And<string, TAnd, TOr>, IAnd<string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<string, TAnd, TOr>
    {
        return new StringLength<TAnd, TOr>(has.AssertionBuilder.AppendCallerMethod(null), has.ConnectorType, has.OtherAssertCondition);
    }
}