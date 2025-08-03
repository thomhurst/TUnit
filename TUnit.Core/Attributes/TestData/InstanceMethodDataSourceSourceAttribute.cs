namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class InstanceMethodDataSourceAttribute(string methodNameProvidingDataSource) : MethodDataSourceAttribute(methodNameProvidingDataSource), IAccessesInstanceData;
