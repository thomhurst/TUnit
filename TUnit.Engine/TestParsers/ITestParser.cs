using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using TUnit.Core;

namespace TUnit.Engine.TestParsers;

internal interface ITestParser
{
    IEnumerable<TestDetails> GetTestCases(MethodInfo methodInfo,
        Type[] nonAbstractClassesContainingTest,
        int runCount,
        SourceLocation sourceLocation
    );
}