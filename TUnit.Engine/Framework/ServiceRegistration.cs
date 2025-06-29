using Microsoft.Extensions.DependencyInjection;
using TUnit.Core;
using TUnit.Engine.Building;
using TUnit.Engine.Building.Collectors;
using TUnit.Engine.Building.Expanders;
using TUnit.Engine.Building.Interfaces;
using TUnit.Engine.Building.Resolvers;
using TUnit.Engine.Services;

namespace TUnit.Engine.Framework;

/// <summary>
/// Service registration helpers for the unified test builder
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    /// Registers services for AOT mode (source generation)
    /// </summary>
    public static IServiceCollection AddUnifiedTestBuilderAot(
        this IServiceCollection services,
        ITestMetadataSource metadataSource)
    {
        // Register metadata source
        services.AddSingleton(metadataSource);
        
        // Register AOT-specific implementations
        services.AddSingleton<ITestDataCollector, AotTestDataCollector>();
        services.AddSingleton<IGenericTypeResolver, AotGenericTypeResolver>();
        
        // Register shared components
        services.AddSingleton<IDynamicDataSourceResolver, DynamicDataSourceResolver>();
        services.AddSingleton<IDataSourceExpander, DataSourceExpander>();
        services.AddSingleton<ITestBuilder>(provider =>
        {
            var testInvoker = provider.GetRequiredService<ITestInvoker>();
            var hookInvoker = provider.GetRequiredService<IHookInvoker>();
            return new TestBuilder(testInvoker, hookInvoker, isAotMode: true);
        });
        
        // Register the pipeline
        services.AddSingleton<UnifiedTestBuilderPipeline>();
        
        // Register the new discovery service
        services.AddSingleton<TestDiscoveryServiceV2>();
        
        return services;
    }
    
    /// <summary>
    /// Registers services for reflection mode
    /// </summary>
    public static IServiceCollection AddUnifiedTestBuilderReflection(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        // Register reflection-specific implementations
        services.AddSingleton<ITestDataCollector>(new ReflectionTestDataCollector(assemblies));
        services.AddSingleton<IGenericTypeResolver>(new GenericTypeResolver(isAotMode: false));
        
        // Register shared components
        services.AddSingleton<IDynamicDataSourceResolver, DynamicDataSourceResolver>();
        services.AddSingleton<IDataSourceExpander, DataSourceExpander>();
        services.AddSingleton<ITestBuilder>(provider =>
        {
            var testInvoker = provider.GetRequiredService<ITestInvoker>();
            var hookInvoker = provider.GetRequiredService<IHookInvoker>();
            return new TestBuilder(testInvoker, hookInvoker, isAotMode: false);
        });
        
        // Register the pipeline
        services.AddSingleton<UnifiedTestBuilderPipeline>();
        
        // Register the new discovery service
        services.AddSingleton<TestDiscoveryServiceV2>();
        
        return services;
    }
}