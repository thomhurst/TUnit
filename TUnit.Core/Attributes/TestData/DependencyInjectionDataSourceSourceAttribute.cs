﻿using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

#if NET6_0_OR_GREATER
[RequiresDynamicCode("DependencyInjectionDataSourceAttribute requires dynamic code generation for dependency injection container access. This attribute is inherently incompatible with AOT compilation.")]
[RequiresUnreferencedCode("DependencyInjectionDataSourceAttribute may require unreferenced code for dependency injection container access. This attribute is inherently incompatible with AOT compilation.")]
#endif
public abstract class DependencyInjectionDataSourceAttribute<TScope> : UntypedDataSourceGeneratorAttribute
{
    protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () =>
        {
            // Create a new scope for each test execution
            var scope = CreateScope(dataGeneratorMetadata);

            // Set up disposal for this specific scope in the current test context
            if (dataGeneratorMetadata.TestBuilderContext != null)
            {
                dataGeneratorMetadata.TestBuilderContext.Current.Events.OnDispose += async (_, _) =>
                {
                    if (scope is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    }
                    else if (scope is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                };
            }

            return dataGeneratorMetadata.MembersToGenerate
                .Select(m => m.Type)
                .Select(x => Create(scope, x))
                .ToArray();
        };
    }

    public abstract TScope CreateScope(DataGeneratorMetadata dataGeneratorMetadata);

    public abstract object? Create(TScope scope, Type type);
}
