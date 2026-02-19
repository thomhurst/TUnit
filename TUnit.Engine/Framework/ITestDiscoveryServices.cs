using TUnit.Core.Interfaces;
using TUnit.Engine.Building;
using TUnit.Engine.Services;

namespace TUnit.Engine.Framework;

/// <summary>
/// Provides access to services related to test discovery and filtering.
/// </summary>
internal interface ITestDiscoveryServices
{
    TestDiscoveryService DiscoveryService { get; }
    TestBuilderPipeline TestBuilderPipeline { get; }
    TestFilterService TestFilterService { get; }
    ITestFinder TestFinder { get; }
}
