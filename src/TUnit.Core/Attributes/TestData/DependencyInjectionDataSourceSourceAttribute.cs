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

            // Method and class arguments are reused by retries; dispose their scope after the final attempt.
            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnDataRowDispose += async (_, _) =>
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
