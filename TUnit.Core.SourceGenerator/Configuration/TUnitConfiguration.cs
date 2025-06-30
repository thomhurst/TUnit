using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Core.SourceGenerator.Configuration;

/// <summary>
/// Configuration for TUnit source generation
/// </summary>
internal class TUnitConfiguration
{
    /// <summary>
    /// Maximum depth for generic type resolution
    /// </summary>
    public int GenericDepthLimit { get; set; } = 5;
    
    /// <summary>
    /// Enable AOT-only mode (no reflection fallback)
    /// </summary>
    public bool AotOnlyMode { get; set; } = false;
    
    /// <summary>
    /// Enable property injection
    /// </summary>
    public bool EnablePropertyInjection { get; set; } = true;
    
    /// <summary>
    /// Enable ValueTask support in hooks
    /// </summary>
    public bool EnableValueTaskHooks { get; set; } = true;
    
    /// <summary>
    /// Enable verbose diagnostics
    /// </summary>
    public bool EnableVerboseDiagnostics { get; set; } = false;
    
    /// <summary>
    /// Maximum number of generic instantiations per type
    /// </summary>
    public int MaxGenericInstantiations { get; set; } = 10;
    
    /// <summary>
    /// Enable automatic generic type discovery
    /// </summary>
    public bool EnableAutoGenericDiscovery { get; set; } = true;
    
    /// <summary>
    /// Creates configuration from analyzer options with validation
    /// </summary>
    public static TUnitConfiguration Create(AnalyzerConfigOptions options)
    {
        var config = new TUnitConfiguration();
        
        // Read from EditorConfig with validation
        if (options.TryGetValue("tunit.generic_depth_limit", out var depthLimit) && 
            int.TryParse(depthLimit, out var depth))
        {
            config.GenericDepthLimit = Math.Max(1, Math.Min(20, depth)); // Clamp between 1-20
        }
        
        if (options.TryGetValue("tunit.aot_only_mode", out var aotOnly) && 
            bool.TryParse(aotOnly, out var aot))
        {
            config.AotOnlyMode = aot;
        }
        
        if (options.TryGetValue("tunit.enable_property_injection", out var propInj) && 
            bool.TryParse(propInj, out var enablePropInj))
        {
            config.EnablePropertyInjection = enablePropInj;
        }
        
        if (options.TryGetValue("tunit.enable_valuetask_hooks", out var vtHooks) && 
            bool.TryParse(vtHooks, out var enableVtHooks))
        {
            config.EnableValueTaskHooks = enableVtHooks;
        }
        
        if (options.TryGetValue("tunit.enable_verbose_diagnostics", out var verbose) && 
            bool.TryParse(verbose, out var enableVerbose))
        {
            config.EnableVerboseDiagnostics = enableVerbose;
        }
        
        if (options.TryGetValue("tunit.max_generic_instantiations", out var maxGeneric) && 
            int.TryParse(maxGeneric, out var max))
        {
            config.MaxGenericInstantiations = Math.Max(1, Math.Min(100, max)); // Clamp between 1-100
        }
        
        if (options.TryGetValue("tunit.enable_auto_generic_discovery", out var autoGeneric) && 
            bool.TryParse(autoGeneric, out var enableAutoGeneric))
        {
            config.EnableAutoGenericDiscovery = enableAutoGeneric;
        }
        
        return config;
    }
    
    /// <summary>
    /// Creates configuration from MSBuild properties
    /// </summary>
    public static TUnitConfiguration Create(Dictionary<string, string> msbuildProperties)
    {
        var config = new TUnitConfiguration();
        
        if (msbuildProperties.TryGetValue("TUnitGenericDepthLimit", out var depthLimit) && 
            int.TryParse(depthLimit, out var depth))
        {
            config.GenericDepthLimit = depth;
        }
        
        if (msbuildProperties.TryGetValue("TUnitAotOnlyMode", out var aotOnly) && 
            bool.TryParse(aotOnly, out var aot))
        {
            config.AotOnlyMode = aot;
        }
        
        if (msbuildProperties.TryGetValue("TUnitEnablePropertyInjection", out var propInj) && 
            bool.TryParse(propInj, out var enablePropInj))
        {
            config.EnablePropertyInjection = enablePropInj;
        }
        
        if (msbuildProperties.TryGetValue("TUnitEnableValueTaskHooks", out var vtHooks) && 
            bool.TryParse(vtHooks, out var enableVtHooks))
        {
            config.EnableValueTaskHooks = enableVtHooks;
        }
        
        if (msbuildProperties.TryGetValue("TUnitEnableVerboseDiagnostics", out var verbose) && 
            bool.TryParse(verbose, out var enableVerbose))
        {
            config.EnableVerboseDiagnostics = enableVerbose;
        }
        
        if (msbuildProperties.TryGetValue("TUnitMaxGenericInstantiations", out var maxGeneric) && 
            int.TryParse(maxGeneric, out var max))
        {
            config.MaxGenericInstantiations = max;
        }
        
        if (msbuildProperties.TryGetValue("TUnitEnableAutoGenericDiscovery", out var autoGeneric) && 
            bool.TryParse(autoGeneric, out var enableAutoGeneric))
        {
            config.EnableAutoGenericDiscovery = enableAutoGeneric;
        }
        
        return config;
    }
}