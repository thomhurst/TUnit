using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7108;

/// <summary>
/// Protected data source members declared on a base class. The generated factory cannot name them, so it reads
/// them by reflection on the declaring base type, which keeps them under native AOT.
/// </summary>
public abstract class InheritedProtectedMemberDataSourceBase
{
    protected static IEnumerable<int> BasePropertyRows => [71];

    protected static readonly int[] BaseFieldRows = [72];

    // Declared as object, so only the runtime value says these are two rows
    protected static readonly object BaseObjectFieldRows = new[] { 73, 74 };
}

[EngineTest(ExpectedResult.Pass)]
public class InheritedProtectedMemberDataSourceTests : InheritedProtectedMemberDataSourceBase
{
    [Test]
    [MethodDataSource(nameof(BasePropertyRows))]
    public async Task Inherited_Protected_Property<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsEqualTo((T)(object)71);
    }

    [Test]
    [MethodDataSource(nameof(BaseFieldRows))]
    public async Task Inherited_Protected_Field<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value).IsEqualTo((T)(object)72);
    }

    [Test]
    [MethodDataSource(nameof(BaseObjectFieldRows))]
    public async Task Inherited_Protected_Object_Field_Yields_A_Row_Per_Element(int value)
    {
        await Assert.That(value).IsBetween(73, 74);
    }
}
