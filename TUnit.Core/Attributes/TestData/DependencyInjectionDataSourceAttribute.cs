using TUnit.Core.Interfaces;

namespace TUnit.Core;

public abstract class DependencyInjectionDataSourceAttribute<TScope> : NonTypedDataSourceGeneratorAttribute
{
    public override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var scope = CreateScope(dataGeneratorMetadata);

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

        yield return () =>
        {
            var objects = dataGeneratorMetadata.MembersToGenerate
                .Select(m => m.Type)
                .Select(x => Create(scope, x))
                .ToArray();
            
            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnInitialize += async (_, _) =>
            {
                foreach (var asyncInitializer in objects.OfType<IAsyncInitializer>())
                {
                    await asyncInitializer.InitializeAsync();
                }
            };
            
            return objects;
        };
    }

    public abstract TScope CreateScope(DataGeneratorMetadata dataGeneratorMetadata);
    
    public abstract object? Create(TScope scope, Type type);
}