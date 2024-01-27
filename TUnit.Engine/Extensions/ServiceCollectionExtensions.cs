using Microsoft.Extensions.DependencyInjection;
using TUnit.Engine;

namespace TUnit.TestAdapter.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTestEngineServices(this IServiceCollection services)
    {
        return services.AddSingleton<MethodInvoker>()
            .AddSingleton<SingleTestExecutor>();
    }
}