using System.Text;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class DependsOnAttribute<T> : DependsOnAttribute
{
    public DependsOnAttribute() : base(typeof(T))
    {
    }

    public DependsOnAttribute(string testName) : base(typeof(T), testName)
    {
    }
    
    public DependsOnAttribute(string testName, Type[] parameterTypes) : base(typeof(T), testName, parameterTypes)
    {
    }
}

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

    public override string ToString()
    {
        if (TestClass != null && TestName == null)
        {
            return TestClass.Name;
        }

        if (ParameterTypes is { Length: > 0 })
        {
            return $"{TestName}({string.Join(", ", ParameterTypes.SelectMany(x => x.Name))}";
        }

        return TestName!;
    }
}