using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

public abstract class DependencyInjectionDataSourceAttribute<TScope> : UntypedDataSourceGeneratorAttribute
{
    protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () =>
        {
            // Create a new scope for each test execution
            var scope = CreateScope(dataGeneratorMetadata);

            // Set up disposal for this specific scope in the current test context
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

            return dataGeneratorMetadata.MembersToGenerate
                .Select(m => m switch
                {
                    PropertyMetadata prop => prop.Type,
                    ParameterMetadata param => param.Type,
                    ClassMetadata cls => cls.Type,
                    MethodMetadata method => method.Type,
                    _ => throw new InvalidOperationException($"Unknown member type: {m.GetType()}")
                })
                .Select(x => Create(scope, x))
                .ToArray();
        };
    }

    public abstract TScope CreateScope(DataGeneratorMetadata dataGeneratorMetadata);

    public abstract object? Create(TScope scope, Type type);
}
