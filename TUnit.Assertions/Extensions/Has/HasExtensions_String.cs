#nullable disable

using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class HasExtensions
{
    public static StringLength<TAnd, TOr> HasLength<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder)
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<string, TAnd, TOr>, IOutputsChain<TOutput, string>
        where TOutput : InvokableAssertionBuilder<string, TAnd, TOr>
    {
        return new StringLength<TAnd, TOr>(assertionBuilder.AppendCallerMethod(null));
    }
}