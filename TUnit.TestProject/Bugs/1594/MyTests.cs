namespace TUnit.TestProject.Bugs._1594;

[ClassDataSource<MyFixture>(Shared = SharedType.None)]
public class MyTests : ParentTests<MyFixture>
{
    public MyTests(MyFixture myFixture)
        : base(myFixture)
    {
        
    }

    [Test]
    public void Test1()
    {
        Console.WriteLine(@"MyTests Class - Test1");
        // I Get output:
        // ParentTests Class - Before Test
        // MyTests Class - Before Test
        // MyTests Class - Test1

        // Expected output:
        // GrandParentTests Class - Before Test
        // ParentTests Class - Before Test
        // MyTests Class - Before Test
        // MyTests Class - Test1
    }

    [Before(Test)]
    public void SetupMyTests()
    {
        Console.WriteLine(@"MyTests Class - Before Test");
    }
}

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