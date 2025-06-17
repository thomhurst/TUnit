namespace TUnit.Core;

public record UntypedTestDetails(ResettableLazy<object> ResettableLazy) : TestDetails
{
    public override object ClassInstance => ResettableLazy.Value;
    
    /// <summary>
    /// Helper method to create UntypedTestDetails with raw Attribute arrays (for backward compatibility)
    /// </summary>
    public static UntypedTestDetails CreateWithRawAttributes(
        ResettableLazy<object> resettableLazy,
        string testId,
        string testName,
        MethodMetadata testMethod,
        string testFilePath,
        int testLineNumber,
        object?[] testClassArguments,
        object?[] testMethodArguments,
        IDictionary<string, object?> testClassInjectedPropertyArguments,
        int repeatLimit,
        int currentRepeatAttempt,
        Type returnType,
        Attribute[] dataAttributes)
    {
        var details = new UntypedTestDetails(resettableLazy)
        {
            TestId = testId,
            TestName = testName,
            MethodMetadata = testMethod,
            TestFilePath = testFilePath,
            TestLineNumber = testLineNumber,
            TestClassArguments = testClassArguments,
            TestMethodArguments = testMethodArguments,
            TestClassInjectedPropertyArguments = testClassInjectedPropertyArguments,
            RepeatLimit = repeatLimit,
            CurrentRepeatAttempt = currentRepeatAttempt,
            ReturnType = returnType,
            DynamicAttributes = [], // Will be set below
            DataAttributes = [] // Will be set below
        };
        
        // Set the raw attributes which will be converted to TestAttributeMetadata on access
        details._rawDynamicAttributes = [];
        details._rawDataAttributes = dataAttributes;
        
        return details;
    }
}