namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class TestDataSourceAttribute : TUnitAttribute
{
    public Type? ClassProvidingDataSource { get; }
    public string MethodNameProvidingDataSource { get; }
    

    public TestDataSourceAttribute(string methodNameProvidingDataSource)
    {
        MethodNameProvidingDataSource = methodNameProvidingDataSource;
    }
    
    public TestDataSourceAttribute(Type classProvidingDataSource, string methodNameProvidingDataSource)
    {
        ClassProvidingDataSource = classProvidingDataSource;
        MethodNameProvidingDataSource = methodNameProvidingDataSource;
    }
}