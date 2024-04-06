using Microsoft.Extensions.DependencyInjection;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Services;
using TUnit.Engine.TestParsers;

namespace TUnit.Engine.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFromFrameworkServiceProvider(this IServiceCollection services,
        IServiceProvider serviceProvider, IExtension extension)
    {
        return services
            .AddSingleton(extension)
            .AddTransient(_ => serviceProvider.GetConfiguration())
            .AddTransient(_ => serviceProvider.GetLoggerFactory())
            .AddTransient(_ => serviceProvider.GetMessageBus())
            .AddTransient(_ => serviceProvider.GetCommandLineOptions())
            .AddTransient(_ => serviceProvider.GetOutputDevice());
    }
        
    public static IServiceCollection AddTestEngineServices(this IServiceCollection services)
    {
        return services
            .AddSingleton(new CancellationTokenSource())
            .AddSingleton<MethodInvoker>()
            .AddSingleton<DataSourceRetriever>()
            .AddSingleton<Disposer>()
            .AddSingleton<GlobalDisposer>()
            .AddSingleton<ConsoleInterceptor>()
            .AddSingleton<TestsLoader>()
            .AddSingleton<ITestParser, BasicTestParser>()
            .AddSingleton<ITestParser, DataDrivenTestsParser>()
            .AddSingleton<ITestParser, DataSourceDrivenTestParser>()
            .AddSingleton<TestsExecutor>()
            .AddSingleton<TestGrouper>()
            .AddSingleton<CombinativeSolver>()
            .AddSingleton<SingleTestExecutor>()
            .AddSingleton<TestInvoker>()
            .AddSingleton<SystemResourceMonitor>()
            .AddSingleton<TUnitTestDiscoverer>()
            .AddSingleton<TestFilterService>();
    }
}