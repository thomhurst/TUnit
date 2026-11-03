namespace TUnit.Inference;

// Compiled into the engine and every generator variant. Adapters extract types and row identities;
// this policy decides ownership and row scope without depending on either type system.
internal static class GenericClassInferencePolicy
{
    internal enum Source
    {
        ClassRow,
        MethodArguments,
        MethodDataSource,
        ParameterAttribute,
        GenerateGenericTest,
    }

    // Class rows include literal, typed and member sources: equal inferred types join the same
    // class instantiation, rather than dropping a later source as generic method inference can.
    // Runtime-only row types are extracted by the reflection/partition adapters, not this table.
    internal static readonly Source[] Sources =
    [
        Source.ClassRow,
        Source.MethodArguments,
        Source.MethodDataSource,
        Source.ParameterAttribute,
        Source.GenerateGenericTest,
    ];

    internal enum RowFallback
    {
        None,
        AllRows,
        NoClassRow,
    }

    internal static bool OwnsInstantiation(Source candidate, Source existing)
        => System.Array.IndexOf(Sources, candidate) <= System.Array.IndexOf(Sources, existing);

    internal static RowFallback SelectRowFallback(bool hasMatchingClassRow, bool hasClassArgumentsRows)
        => hasMatchingClassRow ? RowFallback.None
            : hasClassArgumentsRows ? RowFallback.NoClassRow
            : RowFallback.AllRows;

    internal static bool IncludesRow(bool hasInferredTypes, bool matchesInstantiation, bool includeUninferred)
        => hasInferredTypes ? matchesInstantiation : includeUninferred;

    internal static bool ClassRowSuppliesNoTypeArguments(int valueCount, int boundCount, bool hasClassTypeParameter)
        => boundCount == 0 ? valueCount == 0 : !hasClassTypeParameter;

    internal static bool NullMethodRowInfersObject(bool hasClassTypeSource, bool hasExplicitClassTypes)
        => !hasClassTypeSource && !hasExplicitClassTypes;

    internal static bool IsCompletePair(int classArity, int methodArity, int typeArgumentCount)
        => classArity == 0 || methodArity == 0 || typeArgumentCount == classArity + methodArity;

    // The class forms the outer substitution scope, followed by the method. Adapters may first
    // substitute class parameters inside method types, but must retain this order for constraints.
    internal static T[] Pair<T>(T[] classTypes, T[] methodTypes)
    {
        var combined = new T[classTypes.Length + methodTypes.Length];
        System.Array.Copy(classTypes, combined, classTypes.Length);
        System.Array.Copy(methodTypes, 0, combined, classTypes.Length, methodTypes.Length);
        return combined;
    }

    // Closing a generic class establishes the outer scope. Within it method source selection has
    // exactly the same precedence and duplicate/fallback policy as on a non-generic class.
    internal static GenericMethodInferencePolicy.Rule[] MethodSources => GenericMethodInferencePolicy.Rules;
}
