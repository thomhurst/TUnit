using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

/// <summary>
/// A method data source attribute that requires an instance of the test class to be created first.
/// This implements IAccessesInstanceData which tells the engine to create a properly-initialized
/// instance before evaluating the data source.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class InstanceMethodDataSourceAttribute : MethodDataSourceAttribute, IAccessesInstanceData
{
    public InstanceMethodDataSourceAttribute(string methodNameProvidingDataSource)
        : base(methodNameProvidingDataSource)
    {
    }

    public InstanceMethodDataSourceAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type classProvidingDataSource,
        string methodNameProvidingDataSource)
        : base(classProvidingDataSource, methodNameProvidingDataSource)
    {
    }
}
