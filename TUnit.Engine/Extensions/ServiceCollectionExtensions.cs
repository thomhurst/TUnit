using Microsoft.Extensions.DependencyInjection;

namespace TUnit.Engine.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTestEngineServices(this IServiceCollection services)
    {
        return services.AddSingleton<MethodInvoker>()
            .AddSingleton<TestClassCreator>()
            .AddSingleton<TestDataSourceRetriever>()
            .AddSingleton<ClassLoader>()
            .AddSingleton<Disposer>()
            .AddSingleton<SingleTestExecutor>();
    }
}