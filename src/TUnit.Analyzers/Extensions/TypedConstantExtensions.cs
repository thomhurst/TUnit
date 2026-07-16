using Microsoft.CodeAnalysis;

namespace TUnit.Analyzers.Extensions;

public static class TypedConstantExtensions
{
    public static IEnumerable<object?> SafeGetValues(this TypedConstant typedConstant)
    {
        if (typedConstant.IsNull
            || typedConstant.Kind == TypedConstantKind.Error)
        {
            return new List<object?>
            {
            }.AsReadOnly();
        }

        if (typedConstant.Kind == TypedConstantKind.Array)
        {
            return typedConstant.Values.SelectMany(x => x.SafeGetValues());
        }

        return new List<object?>
        {
            typedConstant.Value
        }.AsReadOnly();
    }
}
