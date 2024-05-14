namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class EnumerableMethodDataSourceAttribute : MethodDataSourceAttribute
{
    public EnumerableMethodDataSourceAttribute(string methodNameProvidingDataSource) : base(methodNameProvidingDataSource)
    {
    }

    public EnumerableMethodDataSourceAttribute(Type classProvidingDataSource, string methodNameProvidingDataSource) : base(classProvidingDataSource, methodNameProvidingDataSource)
    {
    }
}