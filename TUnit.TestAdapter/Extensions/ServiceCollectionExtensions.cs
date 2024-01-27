using Microsoft.Extensions.DependencyInjection;
using TUnit.Engine;

namespace TUnit.TestAdapter.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTestAdapterServices(this IServiceCollection services)
    {
        return services.AddSingleton<TestsLoader>()
            .AddSingleton<AsyncTestRunExecutor>()
            .AddSingleton<SingleTestExecutor>()
            .AddSingleton<TestCollector>()
            .AddSingleton<SourceLocationHelper>()
            .AddSingleton<ReflectionMetadataProvider>()
            .AddSingleton<AssemblyLoader>();
    }
}