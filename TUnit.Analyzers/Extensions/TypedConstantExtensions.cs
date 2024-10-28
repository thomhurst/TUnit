using Microsoft.CodeAnalysis;

namespace TUnit.Analyzers.Extensions;

public static class TypedConstantExtensions
{
    public static IEnumerable<object?> SafeGetValues(this TypedConstant typedConstant)
    {
        if (typedConstant.IsNull
            || typedConstant.Kind == TypedConstantKind.Error)
        {
            return [];
        }

        if (typedConstant.Kind == TypedConstantKind.Array)
        {
            return typedConstant.Values.SelectMany(x => x.SafeGetValues());
        }

        return [typedConstant.Value];
    }
}