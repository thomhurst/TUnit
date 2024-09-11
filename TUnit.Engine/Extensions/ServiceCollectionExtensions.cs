using Microsoft.Extensions.DependencyInjection;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Services;
using TUnit.Engine.Helpers;
using TUnit.Engine.Hooks;
using TUnit.Engine.Logging;
using TUnit.Engine.Services;

namespace TUnit.Engine.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFromFrameworkServiceProvider(this IServiceCollection services,
        IServiceProvider frameworkServiceProvider, IExtension extension)
    {
        return services
            .AddSingleton(extension)
            .AddTransient(_ => frameworkServiceProvider.GetCommandLineOptions());
    }
        
    public static IServiceCollection AddTestEngineServices(this IServiceCollection services,
        IServiceProvider frameworkServiceProvider, IExtension extension)
    {
        return services
            .AddSingleton(EngineCancellationToken.CancellationTokenSource)
            .AddSingleton<Disposer>()
            .AddSingleton<StandardOutConsoleInterceptor>()
            .AddSingleton<StandardErrorConsoleInterceptor>()
            .AddSingleton<TestsLoader>()
            .AddSingleton<TestsExecutor>()
            .AddSingleton<TestGrouper>()
            .AddSingleton<SingleTestExecutor>()
            .AddSingleton<TestInvoker>()
            .AddSingleton<TUnitTestDiscoverer>()
            .AddSingleton<TestFilterService>(_ => new TestFilterService(frameworkServiceProvider.GetLoggerFactory()))
            .AddSingleton<ExplicitFilterService>()
            .AddSingleton<OnEndExecutor>()
            .AddSingleton<TUnitLogger>(_ => new TUnitLogger(extension, frameworkServiceProvider.GetOutputDevice(), frameworkServiceProvider.GetLoggerFactory()))
            .AddSingleton<TUnitInitializer>()
            .AddSingleton<ParallelLimitProvider>()
            .AddSingleton<AssemblyHookOrchestrator>()
            .AddSingleton<ClassHookOrchestrator>()
            .AddSingleton<GlobalStaticTestHookOrchestrator>()
            .AddSingleton<HookMessagePublisher>();
    }
}