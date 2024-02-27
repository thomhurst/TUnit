using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Requests;

namespace TUnit.Engine;

public class BasicFilter : ITestExecutionFilter
{
    public string Filter { get; }

    internal BasicFilter(string filter)
    {
        ArgumentException.ThrowIfNullOrEmpty(filter);
        Filter = filter;
    }

    public bool MatchesFilter(TestNode testNode)
    {
        return true;
    }
}