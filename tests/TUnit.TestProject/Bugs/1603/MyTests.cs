using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._1603;

[EngineTest(ExpectedResult.Pass)]
public abstract class ParentTests<TFixture> : GrandParentTests<TFixture>
    where TFixture : BaseFixture
{
    public ParentTests(TFixture fixture)
        : base(fixture)
    {
    }

    [Before(Test)] // This line is causing another Generated code error
    public void SetupParentTests()
    {
        Console.WriteLine(@"ParentTests Class - Before Test");
    }
}

public abstract class GrandParentTests<TFixture>
    where TFixture : BaseFixture
{
    public TFixture Fixture { get; set; }

    public GrandParentTests(TFixture fixture)
    {
        Fixture = fixture;
    }

    [Before(Test)]
    public void SetupBase()
    {
        Console.WriteLine(@"GrandParentTests Class - Before Test");
    }
}

public class MyFixture : BaseFixture
{
    public int MyProperty { get; set; }
}

public class BaseFixture
{
    public int BaseProperty { get; set; }
}
