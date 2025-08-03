using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class EnumerableDataSourceDrivenTests
{
    [Test]
    [MethodDataSource(nameof(SomeMethod))]
    public async Task DataSource_Method(int value)
    {
        await Assert.That(value).IsBetween(1, 5).WithInclusiveBounds();
    }

    [Test]
    [MethodDataSource(nameof(SomeMethod))]
    public async Task DataSource_Method2(int value)
    {
        await Assert.That(value).IsBetween(1, 5).WithInclusiveBounds();
    }

    [Test]
    [MethodDataSource(nameof(MethodWithBaseReturn))]
    public void DataSource_WithBaseReturn(BaseValue value)
    {
    }

    public static IEnumerable<int> SomeMethod() => [1, 2, 3, 4, 5];

    public static List<Func<BaseValue>> MethodWithBaseReturn() =>
    [
        () => new ConcreteValue(),
        () => new ConcreteValue2()
    ];

    public abstract class BaseValue;

    public class ConcreteValue : BaseValue;
    public class ConcreteValue2 : BaseValue;
}
