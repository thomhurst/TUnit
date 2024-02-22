using Microsoft.Extensions.DependencyInjection;
using TUnit.Engine.TestParsers;

namespace TUnit.Engine.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTestEngineServices(this IServiceCollection services)
    {
        return services.AddSingleton<MethodInvoker>()
            .AddSingleton<DataSourceRetriever>()
            .AddSingleton<Disposer>()
            .AddSingleton<CacheableAssemblyLoader>()
            .AddSingleton<ConsoleInterceptor>()
            .AddSingleton<TestsLoader>()
            .AddSingleton<ITestParser, BasicTestParser>()
            .AddSingleton<ITestParser, DataDrivenTestParser>()
            .AddSingleton<ITestParser, DataSourceDrivenTestParser>()
            .AddSingleton<AsyncTestRunExecutor>()
            .AddSingleton<TestCollector>()
            .AddSingleton<SourceLocationRetriever>()
            .AddSingleton<ReflectionMetadataProvider>()
            .AddSingleton<TestFilterProvider>()
            .AddSingleton<TestClassCreator>()
            .AddSingleton<TestMethodRetriever>()
            .AddSingleton<TestGrouper>()
            .AddSingleton<CombinativeSolver>()
            .AddSingleton<SingleTestExecutor>()
            .AddSingleton<SystemResourceMonitor>()
            .AddSingleton<ClassWalker>()
            .AddSingleton<AssemblySetUpExecutor>()
            .AddSingleton<AssemblyCleanUpExecutor>();
    }
}