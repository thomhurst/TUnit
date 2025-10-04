using System.Diagnostics.CodeAnalysis;
using TUnit.Core;

namespace TUnit.Engine.Building.Interfaces;

/// <summary>
/// Interface for collecting test metadata from various sources (AOT or reflection)
/// </summary>
internal interface ITestDataCollector
{
    /// <summary>
    /// Collects all test metadata from the configured source
    /// </summary>
    /// <returns>Collection of test metadata ready for processing</returns>
    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Reflection-based implementation uses assembly scanning")]
    [RequiresDynamicCode("Reflection-based implementation uses MakeGenericType")]
    #endif
    Task<IEnumerable<TestMetadata>> CollectTestsAsync(string testSessionId);
}
