using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;

namespace TUnit.Engine;

public class TestFilterService
{
    private readonly ILogger<TestFilterService> _logger;

    public TestFilterService(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<TestFilterService>();
    }
    
    public IEnumerable<TestInformation> FilterTests(ITestExecutionFilter? testExecutionFilter, IEnumerable<TestInformation> testNodes)
    {
        if (testExecutionFilter is null)
        {
            return testNodes;
        }

        return testNodes.Where(x => MatchesTest(testExecutionFilter, x));
    }

    public bool MatchesTest(ITestExecutionFilter? testExecutionFilter, TestInformation testInformation)
    {
        return testExecutionFilter switch
        {
            null => true,
            TestNodeUidListFilter testNodeUidListFilter => testNodeUidListFilter.TestNodeUids.Contains(new TestNodeUid(testInformation.TestId)),
            _ => UnhandledFilter(testExecutionFilter)
        };
    }

    private bool UnhandledFilter(ITestExecutionFilter testExecutionFilter)
    {
        _logger.LogWarning($"Filter is Unhandled Type: {testExecutionFilter.GetType().FullName}");
        return true;
    }
}