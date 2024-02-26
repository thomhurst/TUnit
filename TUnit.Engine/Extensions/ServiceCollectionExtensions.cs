using Microsoft.Extensions.DependencyInjection;
using Microsoft.Testing.Platform.Services;
using TUnit.Engine.TestParsers;

namespace TUnit.Engine.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFromFrameworkServiceProvider(this IServiceCollection services, IServiceProvider serviceProvider)
    {
        return services
            .AddTransient(_ => serviceProvider.GetConfiguration())
            .AddTransient(_ => serviceProvider.GetLoggerFactory())
            .AddTransient(_ => serviceProvider.GetMessageBus())
            .AddTransient(_ => serviceProvider.GetCommandLineOptions())
            .AddTransient(_ => serviceProvider.GetOutputDevice());
    }
        
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
            .AddSingleton<TestClassCreator>()
            .AddSingleton<TestMethodRetriever>()
            .AddSingleton<TestGrouper>()
            .AddSingleton<CombinativeSolver>()
            .AddSingleton<SingleTestExecutor>()
            .AddSingleton<SystemResourceMonitor>()
            .AddSingleton<ClassWalker>()
            .AddSingleton<AssemblySetUpExecutor>()
            .AddSingleton<AssemblyCleanUpExecutor>()
            .AddSingleton<AssemblyLoader>()
            .AddSingleton<TUnitTestDiscoverer>();
    }
}