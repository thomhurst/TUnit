namespace TUnit.Core;

public class DependsOnAttribute : TUnitAttribute
{
    public string TestName { get; }
    public Type[] ParameterTypes { get; }

    public DependsOnAttribute(string testName) : this(testName, [])
    {
    }
    
    public DependsOnAttribute(string testName, Type[] parameterTypes)
    {
        TestName = testName;
        ParameterTypes = parameterTypes;
    }
}