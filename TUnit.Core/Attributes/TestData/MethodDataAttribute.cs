namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class MethodDataAttribute : TUnitAttribute
{
    public Type? ClassProvidingDataSource { get; }
    public string MethodNameProvidingDataSource { get; }

    public MethodDataAttribute(string methodNameProvidingDataSource)
    {
        ArgumentException.ThrowIfNullOrEmpty(methodNameProvidingDataSource);
        MethodNameProvidingDataSource = methodNameProvidingDataSource;
    }
    
    public MethodDataAttribute(Type classProvidingDataSource, 
        string methodNameProvidingDataSource)
    {
        ArgumentNullException.ThrowIfNull(classProvidingDataSource);
        ArgumentException.ThrowIfNullOrEmpty(methodNameProvidingDataSource);

        ClassProvidingDataSource = classProvidingDataSource;
        MethodNameProvidingDataSource = methodNameProvidingDataSource;
    }
}