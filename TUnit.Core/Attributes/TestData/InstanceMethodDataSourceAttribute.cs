namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class InstanceMethodDataSourceAttribute : MethodDataSourceAttribute, IAccessesInstanceData
{
    public InstanceMethodDataSourceAttribute(string methodNameProvidingDataSource) : base(methodNameProvidingDataSource)
    {
    }

    public InstanceMethodDataSourceAttribute(Type classProvidingDataSource, string methodNameProvidingDataSource) : base(classProvidingDataSource, methodNameProvidingDataSource)
    {
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class InstanceMethodDataSourceAttribute<T>(string methodNameProvidingDataSource) : MethodDataSourceAttribute<T>(methodNameProvidingDataSource), IAccessesInstanceData;