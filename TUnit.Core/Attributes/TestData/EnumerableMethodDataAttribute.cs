namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class EnumerableMethodDataAttribute : MethodDataAttribute
{
    public EnumerableMethodDataAttribute(string methodNameProvidingDataSource) : base(methodNameProvidingDataSource)
    {
    }

    public EnumerableMethodDataAttribute(Type classProvidingDataSource, string methodNameProvidingDataSource) : base(classProvidingDataSource, methodNameProvidingDataSource)
    {
    }
}