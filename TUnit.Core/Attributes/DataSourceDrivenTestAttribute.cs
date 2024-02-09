namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class DataSourceDrivenTestAttribute : TUnitAttribute
{
    public Type? ClassProvidingDataSource { get; }
    public string MethodNameProvidingDataSource { get; }
    

    public DataSourceDrivenTestAttribute(string methodNameProvidingDataSource)
    {
        MethodNameProvidingDataSource = methodNameProvidingDataSource;
    }
    
    public DataSourceDrivenTestAttribute(Type classProvidingDataSource, string methodNameProvidingDataSource)
    {
        ClassProvidingDataSource = classProvidingDataSource;
        MethodNameProvidingDataSource = methodNameProvidingDataSource;
    }
}