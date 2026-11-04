using TUnit.Core.Helpers;

namespace TUnit.Engine.Helpers;

internal static class TupleArguments
{
    // Keep the binding arguments intact, but expose retained tuple elements to services that
    // previously saw them as separate data-row arguments. Ordinary rows allocate nothing.
    public static object?[] Expand(object?[] arguments)
    {
        if (!Array.Exists(arguments, static argument => argument is not null && (argument is TupleArgument || TupleHelper.IsTupleType(argument))))
        {
            return arguments;
        }

        var expanded = new List<object?>();
        var visited = new HashSet<object>(TUnit.Core.Helpers.ReferenceEqualityComparer.Instance);
        foreach (var argument in arguments)
        {
            Add(argument, 0);
        }
        return expanded.ToArray();

        void Add(object? argument, int depth)
        {
            if (argument is null || !visited.Add(argument))
            {
                return;
            }

            expanded.Add(argument);
            if (depth >= 50)
            {
                return;
            }
            if (argument is TupleArgument retained)
            {
                foreach (var element in retained.Elements)
                {
                    Add(element, depth + 1);
                }
                return;
            }
#if NET
            if (argument is System.Runtime.CompilerServices.ITuple tuple)
            {
                for (var i = 0; i < tuple.Length; i++)
                {
                    Add(tuple[i], depth + 1);
                }
            }
#else
            if (TupleHelper.IsTupleType(argument))
            {
                foreach (var element in DataSourceHelpers.UnwrapTupleAot(argument))
                {
                    Add(element, depth + 1);
                }
            }
#endif
        }
    }
}
