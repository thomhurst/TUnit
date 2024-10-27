using Microsoft.Testing.Platform.Logging;
using TUnit.Core;

namespace TUnit.Engine.Services;

internal class TestMetadataCollector(string sessionId, ITUnitMessageBus messageBus, ILoggerFactory loggerFactory)
{
    private readonly ILogger<TestsConstructor> _logger = loggerFactory.CreateLogger<TestsConstructor>();

    public IEnumerable<TestMetadata> GetTests()
    {
        var count = 0;
        
        foreach (var sourceGeneratedTestNode in Sources.TestSources
                     .AsParallel()
                     .SelectMany(x => x.CollectTests(sessionId)))
        {
            count++;

            if (sourceGeneratedTestNode.FailedInitializationTest is {} failedInitializationTest)
            {
                messageBus.FailedInitialization(failedInitializationTest);
                continue;
            }
            
            yield return sourceGeneratedTestNode.TestMetadata!;
        }
        
        _logger.LogTrace($"Found {count} before filtering.");
    }
}