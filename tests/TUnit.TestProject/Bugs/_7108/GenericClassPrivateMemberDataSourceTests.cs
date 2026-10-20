using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7108;

/// <summary>
/// A private data source field or property declared on a generic class. The generated factory cannot read
/// it by reflection on a declaring type that has type parameters, so the attribute keeps its reflection
/// fallback. The rows must still reach the tests in both modes (#7108).
/// A generic method on a generic class is not covered here: it does not compile in source-gen mode yet (#7117).
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(string))]
public class GenericClassPrivateMemberDataSourceTests<TClass>
{
    private static readonly int[] PrivateFieldRows = [51];

    private static IEnumerable<int> PrivatePropertyRows => [52];

    [Test]
    [MethodDataSource(nameof(PrivateFieldRows))]
    public async Task Private_Field(int value)
    {
        await Assert.That(value).IsEqualTo(51);
        await Assert.That(typeof(TClass)).IsEqualTo(typeof(string));
    }

    [Test]
    [MethodDataSource(nameof(PrivatePropertyRows))]
    public async Task Private_Property(int value)
    {
        await Assert.That(value).IsEqualTo(52);
        await Assert.That(typeof(TClass)).IsEqualTo(typeof(string));
    }
}
