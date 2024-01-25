using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using TUnit.Core;
using TUnit.TestAdapter.Constants;

namespace TUnit.TestAdapter.Extensions;

public static class TestExtensions
{
    public static TestCase ToTestCase(this Test test)
    {
        return new TestCase(test.FullName, TestAdapterConstants.ExecutorUri, test.Source)
        {
            DisplayName = test.TestName,
            Id = test.Id,
            CodeFilePath = test.FileName,
            LineNumber = test.MinLineNumber
        };
    }
}