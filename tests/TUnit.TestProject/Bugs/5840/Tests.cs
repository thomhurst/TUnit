using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._5840;

[EngineTest(ExpectedResult.Pass)]
public class Issue5840NestedPropertyCacheTests
{
    [ClassDataSource<Issue5840OuterFixture>(Shared = SharedType.None)]
    public required Issue5840OuterFixture First { get; init; }

    [ClassDataSource<Issue5840OuterFixture>(Shared = SharedType.None)]
    public required Issue5840OuterFixture Second { get; init; }

    [Test]
    public async Task Nested_ClassDataSource_Properties_Are_Isolated_Per_Parent_Instance()
    {
        await Assert.That(First).IsNotSameReferenceAs(Second);
        await Assert.That(First.Inner).IsNotSameReferenceAs(Second.Inner);
    }
}

public class Issue5840OuterFixture
{
    [ClassDataSource<Issue5840InnerFixture>(Shared = SharedType.None)]
    public required Issue5840InnerFixture Inner { get; init; }
}

public class Issue5840InnerFixture;
