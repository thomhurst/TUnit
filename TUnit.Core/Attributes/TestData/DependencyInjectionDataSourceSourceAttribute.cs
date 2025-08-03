using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

[RequiresDynamicCode("DependencyInjectionDataSourceAttribute requires dynamic code generation for dependency injection container access. This attribute is inherently incompatible with AOT compilation.")]
[RequiresUnreferencedCode("DependencyInjectionDataSourceAttribute may require unreferenced code for dependency injection container access. This attribute is inherently incompatible with AOT compilation.")]
public abstract class DependencyInjectionDataSourceAttribute<TScope> : UntypedDataSourceGeneratorAttribute
{
    protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var scope = CreateScope(dataGeneratorMetadata);

        if (dataGeneratorMetadata.TestBuilderContext != null)
        {
            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnDispose += async (_, _) =>
            {
                if (scope is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else if (scope is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            };
        }

        yield return () =>
        {
            return dataGeneratorMetadata.MembersToGenerate
                .Select(m => m.Type)
                .Select(x => Create(scope, x))
                .ToArray();
        };
    }

    public abstract TScope CreateScope(DataGeneratorMetadata dataGeneratorMetadata);

    public abstract object? Create(TScope scope, Type type);
}
