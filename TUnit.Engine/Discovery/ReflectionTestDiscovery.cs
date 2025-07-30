using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Attributes;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Discovery;

/// <summary>
/// Reflection-based test discovery for runtime scenarios.
/// This implementation is not AOT-compatible.
/// </summary>
[RequiresReflection("Test discovery through reflection requires runtime type analysis", 
    AotAlternative = "Use source-generated test discovery with AotTestDiscovery")]
[RequiresUnreferencedCode("Reflection-based test discovery is not compatible with trimming")]
[RequiresDynamicCode("Reflection-based test discovery may require runtime code generation")]
public sealed class ReflectionTestDiscovery : ITestDiscovery
{
    private readonly ReflectionTestDataCollector _collector;
    
    /// <inheritdoc />
    public bool SupportsAot => false;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ReflectionTestDiscovery"/> class.
    /// </summary>
    public ReflectionTestDiscovery()
    {
        _collector = new ReflectionTestDataCollector();
    }
    
    /// <inheritdoc />
    public IEnumerable<TestMetadata> DiscoverTests(Assembly assembly)
    {
        var testData = _collector.GetTests(Guid.NewGuid().ToString(), new[] { assembly }).Result;
        
        foreach (var test in testData.Tests)
        {
            yield return test.TestMetadata;
        }
    }
}