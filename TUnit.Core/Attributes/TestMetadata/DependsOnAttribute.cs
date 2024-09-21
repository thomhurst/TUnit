namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class DependsOnAttribute : TUnitAttribute
{
    public string TestName { get; }
    public Type[] ParameterTypes { get; }
    
    public bool ProceedOnFailure { get; set; }

    public DependsOnAttribute(string testName) : this(testName, [])
    {
    }
    
    public DependsOnAttribute(string testName, Type[] parameterTypes)
    {
        TestName = testName;
        ParameterTypes = parameterTypes;
    }
}