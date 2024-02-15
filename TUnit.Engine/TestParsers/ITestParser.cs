using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine.TestParsers;

internal interface ITestParser
{
    IEnumerable<TestDetails> GetTestCases(MethodInfo methodInfo,
        Type type,
        int runCount,
        SourceLocation sourceLocation
    );
}