using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

public class DynamicTestBuilderContext(string filePath, int lineNumber)
{
    public List<DynamicTest> Tests { get; } = [];

    public void AddTest<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors 
                                    | DynamicallyAccessedMemberTypes.PublicMethods 
                                    | DynamicallyAccessedMemberTypes.PublicProperties)]
        TClass>(DynamicTest<TClass> dynamicTest) where TClass : class
    {
        Tests.Add(dynamicTest with
        {
            TestFilePath = filePath,
            TestLineNumber = lineNumber
        });
    }
}