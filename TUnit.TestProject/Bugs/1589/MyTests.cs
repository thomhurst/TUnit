using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._1589;

[EngineTest(ExpectedResult.Pass)]
[ClassDataSource<MyFixture>(Shared = SharedType.None)]
public class MyTests : BaseTests<MyFixture>
{
    public MyTests(MyFixture myFixture)
        : base(myFixture)
    {

    }

    [Test]
    public async Task Test1()
    {
        var a = true;
        await Assert.That(a).IsTrue();
    }
}

public class BaseTests<TFixture>
    where TFixture : BaseFixture
{
    public TFixture Fixture { get; set; }
    public int MyProp { get; private set; }

    public BaseTests(TFixture fixture)
    {
        Fixture = fixture;
    }

    [Before(Test)] // This line is causing another Generated code error
    public void Setup()
    {
        MyProp = Fixture.BaseProperty;
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