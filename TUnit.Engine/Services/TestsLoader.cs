using Microsoft.Testing.Platform.Logging;
using TUnit.Core;

namespace TUnit.Engine.Services;

internal class TestsLoader
{
    private readonly ILogger<TestsLoader> _logger;

    public TestsLoader(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<TestsLoader>();
    }
    
    public IReadOnlyCollection<DiscoveredTest> GetTests()
    {
        var tests = TestDictionary.GetAllTests();
        
        _logger.LogTrace($"Found {tests.Count} before filtering.");
        
        return tests;
    }
}