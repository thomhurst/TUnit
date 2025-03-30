using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

public class DynamicTestBuilderContext
{
    private readonly string _filePath;
    private readonly int _lineNumber;

    public DynamicTestBuilderContext(string filePath, int lineNumber)
    {
        _filePath = filePath;
        _lineNumber = lineNumber;
    }

    public DynamicTestBuilderContext(TestContext testContext) : this(testContext.TestDetails.TestFilePath, testContext.TestDetails.TestLineNumber)
    {
    }

    public List<DynamicTest> Tests { get; } = [];

    public void AddTest<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors 
                                    | DynamicallyAccessedMemberTypes.PublicMethods 
                                    | DynamicallyAccessedMemberTypes.PublicProperties)]
        TClass>(DynamicTest<TClass> dynamicTest) where TClass : class
    {
        var testToRegister = dynamicTest with
        {
            TestFilePath = _filePath,
            TestLineNumber = _lineNumber
        };
        
        Tests.Add(testToRegister);
    }

    public async Task AddTestAtRuntime<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors 
                                    | DynamicallyAccessedMemberTypes.PublicMethods
                                    | DynamicallyAccessedMemberTypes.PublicProperties)]
        TClass>(TestContext testContext, DynamicTest<TClass> dynamicTest) where TClass : class
    {
        await testContext.GetService<IDynamicTestRegistrar>().Register(dynamicTest);
    }
}