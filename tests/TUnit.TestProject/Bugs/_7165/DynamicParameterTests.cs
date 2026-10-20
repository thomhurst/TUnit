using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7165;

// `dynamic` test parameters, constructor parameters and data-source properties used to generate
// `typeof(dynamic)`, which does not compile (CS1962). Values are read back through `object` so the
// test bodies stay statically bound (#7165).

[EngineTest(ExpectedResult.Pass)]
[Arguments("ctor")]
public class DynamicParameterTests(dynamic constructorValue)
{
    [MethodDataSource(nameof(PropertyValue))]
    public required dynamic Property { get; init; }

    public static string PropertyValue() => "property";

    public static IEnumerable<Func<(List<dynamic>, dynamic[])>> CollectionData()
    {
        yield return () => ([1, "two"], ["three"]);
    }

    [Test]
    [Arguments("x")]
    [Arguments(5)]
    public async Task Dynamic_Parameter_Receives_Argument(dynamic value)
    {
        object received = value;
        await Assert.That(received is "x" or 5).IsTrue();
    }

    [Test]
    [Arguments(1, "a", "b")]
    public async Task Dynamic_Params_Array_Receives_Arguments(int first, params dynamic[] values)
    {
        await Assert.That(first).IsEqualTo(1);
        await Assert.That(values.Cast<object>()).IsEquivalentTo(new object[] { "a", "b" });
    }

    [Test]
    [MethodDataSource(nameof(CollectionData))]
    public async Task Dynamic_Generic_And_Array_Parameters_Receive_Data(List<dynamic> list, dynamic[] array)
    {
        await Assert.That(list.Cast<object>()).IsEquivalentTo(new object[] { 1, "two" });
        await Assert.That(array.Cast<object>()).IsEquivalentTo(new object[] { "three" });
    }

    [Test]
    public async Task Dynamic_Constructor_Parameter_And_Property_Are_Set()
    {
        object constructor = constructorValue;
        object property = Property;
        await Assert.That(constructor).IsEqualTo("ctor");
        await Assert.That(property).IsEqualTo("property");
    }
}
