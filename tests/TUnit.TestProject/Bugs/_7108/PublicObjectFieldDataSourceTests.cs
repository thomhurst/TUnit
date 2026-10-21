using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7108;

/// <summary>
/// A field declared as <c>object</c> does not say whether it holds one row or many, so both modes decide from the
/// value it holds at run time: an array gives one row per element.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class PublicObjectFieldDataSourceTests
{
    public static readonly object StaticObjectFieldRows = new[] { 51, 52 };

    public readonly object InstanceObjectFieldRows = new[] { 53, 54 };

    [Test]
    [MethodDataSource(nameof(StaticObjectFieldRows))]
    public async Task Static_Object_Field_Holding_An_Array_Yields_A_Row_Per_Element(int value)
    {
        await Assert.That(value).IsBetween(51, 52);
    }

    [Test]
    [MethodDataSource(nameof(InstanceObjectFieldRows))]
    public async Task Instance_Object_Field_Holding_An_Array_Yields_A_Row_Per_Element(int value)
    {
        await Assert.That(value).IsBetween(53, 54);
    }
}
