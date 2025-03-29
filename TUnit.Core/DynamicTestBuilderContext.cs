namespace TUnit.Core;

public class DynamicTestBuilderContext(string filePath, int lineNumber)
{
    public List<DynamicTest> Tests { get; } = [];

    public void AddTest<TClass>(DynamicTest<TClass> dynamicTest) where TClass : class
    {
        Tests.Add(dynamicTest with
        {
            TestFilePath = filePath,
            TestLineNumber = lineNumber
        });
    }
}