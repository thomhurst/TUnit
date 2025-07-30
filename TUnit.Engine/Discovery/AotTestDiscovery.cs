using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Attributes;
using TUnit.Core.Interfaces;
using TUnit.Core.Interfaces.SourceGenerator;

namespace TUnit.Engine.Discovery;

/// <summary>
/// AOT-compatible test discovery that uses source-generated metadata.
/// </summary>
[AotCompatible]
public sealed class AotTestDiscovery : ITestDiscovery
{
    /// <inheritdoc />
    public bool SupportsAot => true;
    
    /// <inheritdoc />
    public IEnumerable<TestMetadata> DiscoverTests(Assembly assembly)
    {
        // Look for the source-generated registry type
        var registryType = assembly.GetType($"{assembly.GetName().Name}.SourceGenerated.TestRegistry");
        if (registryType == null)
        {
            // Try alternative naming patterns
            registryType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "TestRegistry" && 
                                   t.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() != null);
        }
        
        if (registryType == null)
        {
            return Enumerable.Empty<TestMetadata>();
        }
        
        // Get the static method that returns test metadata
        var getTestsMethod = registryType.GetMethod("GetTests", BindingFlags.Public | BindingFlags.Static);
        if (getTestsMethod == null)
        {
            return Enumerable.Empty<TestMetadata>();
        }
        
        // Invoke the method to get test metadata
        var tests = getTestsMethod.Invoke(null, null) as IEnumerable<TestMetadata>;
        return tests ?? Enumerable.Empty<TestMetadata>();
    }
    
    /// <summary>
    /// Checks if the assembly has source-generated test metadata.
    /// </summary>
    public static bool HasSourceGeneratedTests(Assembly assembly)
    {
        var registryType = assembly.GetType($"{assembly.GetName().Name}.SourceGenerated.TestRegistry");
        if (registryType != null)
        {
            return true;
        }
        
        // Check for any type with the expected attributes
        return assembly.GetTypes()
            .Any(t => t.Name == "TestRegistry" && 
                     t.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() != null);
    }
}