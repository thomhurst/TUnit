using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Engine.Building.Interfaces;

namespace TUnit.Engine.Building.Resolvers;

/// <summary>
/// Resolves generic types in test metadata before test expansion
/// </summary>
public sealed class GenericTypeResolver : IGenericTypeResolver
{
    private readonly bool _isAotMode;

    public GenericTypeResolver(bool isAotMode = true)
    {
        _isAotMode = isAotMode;
    }

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling", Justification = "Calls to ExpandGenericTestAsync are guarded by _isAotMode check and only occur in reflection mode")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' may break functionality when trimming application code", Justification = "Generic expansion in reflection mode requires dynamic type access which is expected in this mode")]
    public async Task<IEnumerable<TestMetadata>> ResolveGenericsAsync(IEnumerable<TestMetadata> metadata)
    {
        Console.WriteLine($"GenericTypeResolver.ResolveGenericsAsync called with {metadata.Count()} tests, isAotMode={_isAotMode}");
        
        var resolvedTests = new List<TestMetadata>();

        foreach (var test in metadata)
        {
            if (!HasGenericTypes(test))
            {
                // No generics to resolve
                resolvedTests.Add(test);
                continue;
            }

            if (_isAotMode)
            {
                // In AOT mode, generic tests should have been expanded at compile time
                // If we get here, it's an error
                throw new InvalidOperationException(
                    $"Generic test '{test.TestName}' reached runtime in AOT mode. " +
                    "Ensure source generators have expanded all generic tests.");
            }

            // In reflection mode, we need to expand generic tests
            var expandedTests = await ExpandGenericTestAsync(test);
            resolvedTests.AddRange(expandedTests);
        }

        return resolvedTests;
    }

    private static bool HasGenericTypes(TestMetadata test)
    {
        return test.GenericTypeInfo != null || test.GenericMethodInfo != null;
    }

    [RequiresDynamicCode("Generic type resolution requires dynamic code generation")]
    [RequiresUnreferencedCode("Generic type resolution may access types not preserved by trimming")]
    private async Task<IEnumerable<TestMetadata>> ExpandGenericTestAsync(TestMetadata genericTest)
    {
        // For generic tests, we need test data to infer types
        // This is a simplified version - full implementation would need to:
        // 1. Get initial data from data sources
        // 2. Infer generic types from the data
        // 3. Create specialized metadata for each type combination

        if (genericTest.DataSources.Length == 0 && genericTest.GenericMethodInfo != null)
        {
            throw new GenericTypeResolutionException(
                $"Generic test method '{genericTest.TestName}' requires test data to infer generic type parameters. " +
                "Add [Arguments] attributes or other data sources.");
        }

        // For now, return empty - this would need full implementation
        return await Task.FromResult(Enumerable.Empty<TestMetadata>());
    }
}

/// <summary>
/// No-op generic type resolver for AOT mode where generics are pre-resolved
/// </summary>
public sealed class AotGenericTypeResolver : IGenericTypeResolver
{
    public Task<IEnumerable<TestMetadata>> ResolveGenericsAsync(IEnumerable<TestMetadata> metadata)
    {
        Console.WriteLine($"AotGenericTypeResolver.ResolveGenericsAsync called with {metadata.Count()} tests");
        
        // In AOT mode, all generics should already be resolved by source generators
        // Just validate and pass through
        foreach (var test in metadata)
        {
            if (test.GenericTypeInfo != null || test.GenericMethodInfo != null)
            {
                Console.WriteLine($"Found generic test in AOT resolver: {test.TestName}");
                throw new InvalidOperationException(
                    $"Generic test '{test.TestName}' found in AOT mode. " +
                    "All generic tests should be expanded by source generators.");
            }
        }

        return Task.FromResult(metadata);
    }
}
