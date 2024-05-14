namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class MethodDataSourceAttribute : TUnitAttribute
{
    public Type? ClassProvidingDataSource { get; }
    public string MethodNameProvidingDataSource { get; }

    public MethodDataSourceAttribute(string methodNameProvidingDataSource)
    {
        ArgumentException.ThrowIfNullOrEmpty(methodNameProvidingDataSource);
        MethodNameProvidingDataSource = methodNameProvidingDataSource;
    }
    
    public MethodDataSourceAttribute(Type classProvidingDataSource, 
        string methodNameProvidingDataSource)
    {
        ArgumentNullException.ThrowIfNull(classProvidingDataSource);
        ArgumentException.ThrowIfNullOrEmpty(methodNameProvidingDataSource);

        ClassProvidingDataSource = classProvidingDataSource;
        MethodNameProvidingDataSource = methodNameProvidingDataSource;
    }
}