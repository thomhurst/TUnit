namespace TUnit.Core;

public record TestInformation
{
    private readonly TestDetails _testDetails;

    internal TestInformation(TestDetails testDetails, object? classInstance)
    {
        _testDetails = testDetails;
        ClassInstance = classInstance;
    }

    public string TestName => _testDetails.TestName;
    
    public object?[]? TestArguments => _testDetails.ArgumentValues;
    
    public List<string> Categories => _testDetails.Categories;
    
    public Type ClassType => _testDetails.ClassType;
    public object? ClassInstance { get; }
    
    public int RepeatCount => _testDetails.RepeatCount;
    public int RetryCount => _testDetails.RetryCount;
    public int CurrentExecutionCount => _testDetails.CurrentExecutionCount;
}