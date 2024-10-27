namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class DependsOnAttribute : TUnitAttribute
{
    public DependsOnAttribute(string testName) : this(testName, null!)
    {
    }

    public DependsOnAttribute(string testName, Type[] parameterTypes) : this(null!, testName, parameterTypes)
    {
    }

    public DependsOnAttribute(Type testClass) : this(testClass, null!)
    {
    }

    public DependsOnAttribute(Type testClass, string testName) : this(testClass, testName, null!)
    {
    }
    
    public DependsOnAttribute(Type testClass, string testName, Type[] parameterTypes)
    {
        TestClass = testClass;
        TestName = testName;
        ParameterTypes = parameterTypes;
    }
    
    public Type? TestClass { get; }

    public string? TestName { get; }
    public Type[]? ParameterTypes { get; }

    public bool ProceedOnFailure { get; set; }
}