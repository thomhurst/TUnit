using System.Reflection;

namespace TUnit.TestProject;

[ClassDataSource<Derived1>]
[ClassDataSource<Derived2>]
public class ClassDataSourceDrivenTests2
{
    private readonly Base _base;

    public ClassDataSourceDrivenTests2(Base @base)
    {
        _base = @base;
    }
    
    [Test]
    public void Base_Derived1()
    {
        // Dummy method
    }

    [Test]
    public void Base_Derived2()
    {
        // Dummy method
    }

    public class Base;
    public class Derived1 : Base;
    public class Derived2 : Base;
}