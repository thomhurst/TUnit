using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7225;

/// <summary>
/// [InstanceMethodDataSource] naming an instance field or property must get a generated factory, like
/// [MethodDataSource] does; otherwise Native AOT falls back to a reflection lookup that finds no member.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class InstanceMethodDataSourceFieldTests
{
    public readonly object Rows = new[] { 53, 54 };

    public IEnumerable<int> PropertyRows => [55, 56];

    public static IEnumerable<int> StaticRows() => [57, 58];

    [Test]
    [InstanceMethodDataSource(nameof(Rows))]
    public async Task Instance_Field(int value)
    {
        await Assert.That(value).IsBetween(53, 54);
    }

    [Test]
    [InstanceMethodDataSource(nameof(PropertyRows))]
    public async Task Instance_Property(int value)
    {
        await Assert.That(value).IsBetween(55, 56);
    }

    [Test]
    [InstanceMethodDataSource(nameof(StaticRows))]
    public async Task Static_Method(int value)
    {
        await Assert.That(value).IsBetween(57, 58);
    }
}
