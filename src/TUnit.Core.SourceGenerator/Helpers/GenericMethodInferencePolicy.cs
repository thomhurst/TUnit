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

    internal readonly struct Rule(Source source, bool scopesRows, bool fallsBackToExplicitTypes = false)
    {
        internal Source Source { get; } = source;
        internal bool ScopesRows { get; } = scopesRows;
        internal bool FallsBackToExplicitTypes { get; } = fallsBackToExplicitTypes;
    }

    // Precedence is table order. The first source owns an instantiation; literal rows always join.
    // Parameter and explicit-type instantiations run all rows, other inferred sources run their own.
    internal static readonly Rule[] Rules =
    [
        new(Source.Arguments, scopesRows: true),
        new(Source.TypedDataSource, scopesRows: true, fallsBackToExplicitTypes: true),
        new(Source.ParameterAttribute, scopesRows: false),
        new(Source.MethodDataSource, scopesRows: true, fallsBackToExplicitTypes: true),
        new(Source.GenerateGenericTest, scopesRows: false),
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

    internal static bool JoinsDuplicate(bool isLiteralRow, bool joinsInferredSources)
        => isLiteralRow || joinsInferredSources;
}
