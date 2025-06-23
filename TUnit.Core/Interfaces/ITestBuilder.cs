using TUnit.Core.Models;

namespace TUnit.Core.Interfaces;

/// <summary>
/// Interface for building tests from test definitions using the unified TestVariation system.
/// </summary>
internal interface ITestBuilder
{
    /// <summary>
    /// Builds tests from discovery results.
    /// </summary>
    /// <param name="discoveryResult">The discovery result containing test metadata</param>
    /// <returns>Built tests and any failures</returns>
    (IReadOnlyList<DiscoveredTest> Tests, IReadOnlyList<DiscoveryFailure> Failures) BuildTests(DiscoveryResult discoveryResult);
    
    /// <summary>
    /// Builds tests from a dynamic test definition.
    /// </summary>
    /// <param name="dynamicTest">The dynamic test to build</param>
    /// <returns>The built tests</returns>
    IEnumerable<DiscoveredTest> BuildTests(DynamicTest dynamicTest);
    
    /// <summary>
    /// Builds a test from a test definition.
    /// </summary>
    /// <param name="definition">The test definition</param>
    /// <returns>The built test</returns>
    IEnumerable<DiscoveredTest> BuildTests(TestDefinition definition);
    
    /// <summary>
    /// Builds a test from a test variation (unified model).
    /// </summary>
    /// <param name="variation">The test variation</param>
    /// <returns>The built test</returns>
    DiscoveredTest BuildTest(TestVariation variation);
}