using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class InstanceMethodDataSourceAttribute : MethodDataSourceAttribute, IAccessesInstanceData
{
    public InstanceMethodDataSourceAttribute(string methodNameProvidingDataSource) : base(methodNameProvidingDataSource)
    {
    }

    public InstanceMethodDataSourceAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type classProvidingDataSource, string methodNameProvidingDataSource) : base(classProvidingDataSource, methodNameProvidingDataSource)
    {
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class InstanceMethodDataSourceAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
T>(string methodNameProvidingDataSource)
    : MethodDataSourceAttribute<T>(methodNameProvidingDataSource), IAccessesInstanceData;
