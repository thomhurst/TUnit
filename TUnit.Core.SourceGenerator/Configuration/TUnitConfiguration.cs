using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Core.SourceGenerator.Configuration;

/// <summary>
/// Simplified configuration for TUnit source generation
/// </summary>
internal class TUnitConfiguration
{
    /// <summary>
    /// Enable verbose diagnostics during source generation
    /// </summary>
    public bool EnableVerboseDiagnostics { get; set; } = false;
    
    // Hardcoded optimal defaults (no longer configurable):
    // - No generic depth limits (unlimited)
    // - AOT mode enabled by default (with runtime reflection override via --reflection flag)
    // - Property injection always enabled
    // - ValueTask support always enabled
    // - No limits on generic instantiations
    // - Auto generic discovery always enabled
    
    /// <summary>
    /// Creates simplified configuration from analyzer options
    /// </summary>
    public static TUnitConfiguration Create(AnalyzerConfigOptions options)
    {
        var config = new TUnitConfiguration();
        
        // Only read verbose diagnostics setting (all other settings are hardcoded for optimal defaults)
        if (options.TryGetValue("tunit.enable_verbose_diagnostics", out var verbose) && 
            bool.TryParse(verbose, out var enableVerbose))
        {
            config.EnableVerboseDiagnostics = enableVerbose;
        }
        
        return config;
    }
    
    /// <summary>
    /// Creates simplified configuration from MSBuild properties
    /// </summary>
    public static TUnitConfiguration Create(Dictionary<string, string> msbuildProperties)
    {
        var config = new TUnitConfiguration();
        
        // Only read verbose diagnostics setting (all other settings are hardcoded for optimal defaults)
        if (msbuildProperties.TryGetValue("TUnitEnableVerboseDiagnostics", out var verbose) && 
            bool.TryParse(verbose, out var enableVerbose))
        {
            config.EnableVerboseDiagnostics = enableVerbose;
        }
        
        return config;
    }
    
    /// <summary>
    /// Gets hardcoded optimal values for framework features (no longer configurable)
    /// </summary>
    public static class OptimalDefaults
    {
        public const bool EnablePropertyInjection = true;
        public const bool EnableValueTaskHooks = true;
        public const bool EnableAutoGenericDiscovery = true;
        public const bool AotOnlyMode = true; // AOT by default, --reflection flag overrides at runtime
        // No limits on generic depth or instantiations
    }
}