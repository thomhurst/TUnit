namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class DependsOnAttribute(string testName, Type[] parameterTypes) : TUnitAttribute
{
    public string TestName { get; } = testName;
    public Type[] ParameterTypes { get; } = parameterTypes;

    public bool ProceedOnFailure { get; set; }

    public DependsOnAttribute(string testName) : this(testName, [])
    {
    }
}