namespace TUnit.Inference;

// This file is linked into the engine as well as every generator variant. Keep it independent of
// Roslyn and reflection: adapters supply type arguments, while this table owns selection policy.
internal static class GenericMethodInferencePolicy
{
    internal enum Source
    {
        Arguments,
        TypedDataSource,
        ParameterAttribute,
        MethodDataSource,
        GenerateGenericTest,
    }

    internal readonly struct Rule(Source source, bool scopesRows, bool joinsDuplicate, bool fallsBackToExplicitTypes = false)
    {
        internal Source Source { get; } = source;
        internal bool ScopesRows { get; } = scopesRows;
        internal bool JoinsDuplicate { get; } = joinsDuplicate;
        internal bool FallsBackToExplicitTypes { get; } = fallsBackToExplicitTypes;
    }

    // Precedence is table order. The first source owns an instantiation; literal rows always join.
    // Parameter and explicit-type instantiations run all rows, other inferred sources run their own.
    internal static readonly Rule[] Rules =
    [
        new(Source.Arguments, scopesRows: true, joinsDuplicate: true),
        new(Source.TypedDataSource, scopesRows: true, joinsDuplicate: false, fallsBackToExplicitTypes: true),
        new(Source.ParameterAttribute, scopesRows: false, joinsDuplicate: false),
        new(Source.MethodDataSource, scopesRows: true, joinsDuplicate: false, fallsBackToExplicitTypes: true),
        new(Source.GenerateGenericTest, scopesRows: false, joinsDuplicate: false),
    ];

    internal static Rule GetRule(Source source)
    {
        foreach (var rule in Rules)
        {
            if (rule.Source == source)
            {
                return rule;
            }
        }

        throw new System.ArgumentOutOfRangeException(nameof(source));
    }

    internal static bool ReportsConstraintViolation(Source source, bool hasExplicitTypes)
        => !hasExplicitTypes || !GetRule(source).FallsBackToExplicitTypes;

    internal static bool JoinsDuplicate(Source source, bool isLiteralRow, bool hasExplicitTypes, bool isGenericClass)
        => isLiteralRow || GetRule(source).JoinsDuplicate || (!hasExplicitTypes && !isGenericClass);
}
