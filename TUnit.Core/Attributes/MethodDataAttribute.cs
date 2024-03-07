namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ClassDataAttribute : TUnitAttribute
{
    public Type Type { get; }

    public ClassDataAttribute(Type type)
    {
        Type = type;
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class MethodDataAttribute : TUnitAttribute
{
    public Type? ClassProvidingDataSource { get; }
    public string MethodNameProvidingDataSource { get; }
    

    public MethodDataAttribute(string methodNameProvidingDataSource)
    {
        MethodNameProvidingDataSource = methodNameProvidingDataSource;
    }
    
    public MethodDataAttribute(Type classProvidingDataSource, string methodNameProvidingDataSource)
    {
        ClassProvidingDataSource = classProvidingDataSource;
        MethodNameProvidingDataSource = methodNameProvidingDataSource;
    }
}