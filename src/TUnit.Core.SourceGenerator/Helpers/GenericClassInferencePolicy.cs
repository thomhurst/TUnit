namespace TUnit.Inference;

// Compiled into the engine and every generator variant. Adapters extract types and row identities;
// this policy decides row scope and type pairing without depending on either type system.
internal static class GenericClassInferencePolicy
{
    internal enum RowFallback
    {
        // Keep the matching constructor-row partition selected by the adapter.
        None,
        // No compile-time match exists; use every class data source at runtime.
        AllRows,
        // Literal class rows exist but none match; emit the missing-class-row diagnostic.
        NoClassRow,
    }

    internal static RowFallback SelectRowFallback(bool hasMatchingClassRow, bool hasClassArgumentsRows)
        => hasMatchingClassRow ? RowFallback.None
            : hasClassArgumentsRows ? RowFallback.NoClassRow
            : RowFallback.AllRows;

    internal static bool IncludesRow(bool? matchesInstantiation, bool includeUninferred)
        => matchesInstantiation ?? includeUninferred;

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
}
