namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public class MethodDataSourceAttribute : TestDataAttribute
{
    public Type? ClassProvidingDataSource { get; }
    public string MethodNameProvidingDataSource { get; }

    public object?[] Arguments { get; set; } = [];
    
    public MethodDataSourceAttribute(string methodNameProvidingDataSource)
    {
        if (methodNameProvidingDataSource is null or { Length: < 1 })
        {
            throw new ArgumentException("No method name was provided");
        }
        
        MethodNameProvidingDataSource = methodNameProvidingDataSource;
    }
    
    public MethodDataSourceAttribute(Type classProvidingDataSource, 
        string methodNameProvidingDataSource)
    {
        if (methodNameProvidingDataSource is null or { Length: < 1 })
        {
            throw new ArgumentException("No method name was provided");
        }

        ClassProvidingDataSource = classProvidingDataSource ?? throw new ArgumentNullException(nameof(classProvidingDataSource), "No class type was provided");
        MethodNameProvidingDataSource = methodNameProvidingDataSource;
    }
}