using System.Diagnostics.CodeAnalysis;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Failure)]
[UnconditionalSuppressMessage("Usage", "TUnit0033:Conflicting DependsOn attributes")]
public class DeepNestedDependencyConflict
{
    [Test]
    [DependsOn(nameof(Test2))]
    public void Test1()
    {
    }

    [Test]
    [DependsOn(nameof(Test3))]
    public void Test2()
    {
    }

    [Test]
    [DependsOn(nameof(Test4))]
    public void Test3()
    {
    }

    [Test]
    [DependsOn(nameof(Test5))]
    public void Test4()
    {
    }

    [Test]
    [DependsOn(nameof(Test6))]
    public void Test5()
    {
    }

    [Test]
    [DependsOn(nameof(Test7))]
    public void Test6()
    {
    }

    [Test]
    [DependsOn(nameof(Test8))]
    public void Test7()
    {
    }

    [Test]
    [DependsOn(nameof(Test9))]
    public void Test8()
    {
    }

    [Test]
    [DependsOn(nameof(Test10))]
    public void Test9()
    {
    }

    [Test]
    [DependsOn(nameof(Test9))]
    public void Test10()
    {
    }
}
