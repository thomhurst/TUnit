namespace TUnit.Core.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class TestDataSourceAttribute : TUnitAttribute
{
    public string? ClassNameProvidingDataSource { get; }
    public string MethodNameProvidingDataSource { get; }
    

    public TestDataSourceAttribute(string methodNameProvidingDataSource)
    {
        MethodNameProvidingDataSource = methodNameProvidingDataSource;
    }
    
    public TestDataSourceAttribute(string classNameProvidingDataSource, string methodNameProvidingDataSource)
    {
        ClassNameProvidingDataSource = classNameProvidingDataSource;
        MethodNameProvidingDataSource = methodNameProvidingDataSource;
    }
}