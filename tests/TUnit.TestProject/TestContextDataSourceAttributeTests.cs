using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class TestContextDataSourceAttributeTests
{
    [Test]
    public async Task NonDataTest_Returns_NoOp_Singletons()
    {
        await Assert.That(TestContext.Current!.MethodDataSource).IsSameReferenceAs(NoDataSource.Instance);
        await Assert.That(TestContext.Current!.ClassDataSource).IsSameReferenceAs(NoDataSource.Instance);
    }

    [Test]
    [Arguments(1)]
    public async Task ArgumentsTest_Exposes_ArgumentsAttribute(int value)
    {
        await Assert.That(TestContext.Current!.MethodDataSource).IsTypeOf<ArgumentsAttribute>();
        await Assert.That(TestContext.Current!.ClassDataSource).IsSameReferenceAs(NoDataSource.Instance);
    }

    [Test]
    [ClassDataSource<KeyedFixture>(Shared = SharedType.Keyed, Key = "MyKey")]
    public async Task MethodLevel_ClassDataSource_Exposes_Shared_And_Key(KeyedFixture fixture)
    {
        var attribute = TestContext.Current!.MethodDataSource as ClassDataSourceAttribute<KeyedFixture>;

        await Assert.That(attribute).IsNotNull();
        await Assert.That(attribute!.Shared).IsEqualTo(SharedType.Keyed);
        await Assert.That(attribute.Key).IsEqualTo("MyKey");
    }

    [Test]
    [ClassDataSource<SharedTypeAwareFixture>(Shared = SharedType.Keyed, Key = "InitKey")]
    public async Task Fixture_Can_Read_Own_Sharing_Scope_In_InitializeAsync(SharedTypeAwareFixture fixture)
    {
        await Assert.That(fixture.ObservedShared).IsEqualTo(SharedType.Keyed);
        await Assert.That(fixture.ObservedKey).IsEqualTo("InitKey");
    }

    public class KeyedFixture;

    public class SharedTypeAwareFixture : IAsyncInitializer
    {
        public SharedType? ObservedShared { get; private set; }
        public string? ObservedKey { get; private set; }

        public Task InitializeAsync()
        {
            var attribute = TestContext.Current?.MethodDataSource as ClassDataSourceAttribute<SharedTypeAwareFixture>;

            ObservedShared = attribute?.Shared;
            ObservedKey = attribute?.Key;

            return Task.CompletedTask;
        }
    }
}

[EngineTest(ExpectedResult.Pass)]
[ClassDataSource<TestContextConstructorDataSourceTests.ConstructorFixture>(Shared = SharedType.PerClass)]
public class TestContextConstructorDataSourceTests(TestContextConstructorDataSourceTests.ConstructorFixture fixture)
{
    [Test]
    public async Task ConstructorInjected_Exposes_ClassDataSource()
    {
        var attribute = TestContext.Current!.ClassDataSource as ClassDataSourceAttribute<ConstructorFixture>;

        await Assert.That(attribute).IsNotNull();
        await Assert.That(attribute!.Shared).IsEqualTo(SharedType.PerClass);
        await Assert.That(TestContext.Current!.MethodDataSource).IsSameReferenceAs(NoDataSource.Instance);
        await Assert.That(fixture).IsNotNull();
    }

    [Test]
    [Arguments(42)]
    public async Task ConstructorInjected_With_MethodArguments_Exposes_Both(int value)
    {
        await Assert.That(TestContext.Current!.ClassDataSource).IsTypeOf<ClassDataSourceAttribute<ConstructorFixture>>();
        await Assert.That(TestContext.Current!.MethodDataSource).IsTypeOf<ArgumentsAttribute>();
    }

    public class ConstructorFixture;
}
