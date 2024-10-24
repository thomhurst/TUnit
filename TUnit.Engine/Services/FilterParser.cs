using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;

namespace TUnit.Engine.Services;

public class FilterParser
{
    private string? _stringFilter;

    public string? GetTestFilter(ExecuteRequestContext context)
    {
        var filter = context.Request switch
        {
            RunTestExecutionRequest runTestExecutionRequest => runTestExecutionRequest.Filter,
            DiscoverTestExecutionRequest discoverTestExecutionRequest => discoverTestExecutionRequest.Filter,
            TestExecutionRequest testExecutionRequest => testExecutionRequest.Filter,
            _ => throw new ArgumentException(nameof(context.Request))
        };

        return _stringFilter ??= StringifyFilter(filter);
    }

#pragma warning disable TPEXP
    private static string? StringifyFilter(ITestExecutionFilter filter)
    {
        return filter switch
        {
            NopFilter => null,
            TestNodeUidListFilter testNodeUidListFilter => string.Join(",",
                testNodeUidListFilter.TestNodeUids.Select(x => x.Value)),
            TreeNodeFilter treeNodeFilter => treeNodeFilter.Filter,
            _ => throw new ArgumentOutOfRangeException(nameof(filter))
        };
    }
#pragma warning restore TPEXP
}