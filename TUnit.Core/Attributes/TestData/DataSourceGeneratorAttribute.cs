namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public abstract class DataSourceGeneratorAttribute : TUnitAttribute
{
    public abstract IEnumerable<object[]> GenerateDataSources(Type[] parameterTypes);
}