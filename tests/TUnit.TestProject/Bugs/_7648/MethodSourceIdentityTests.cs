using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7648;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class MethodDataSourceAttribute(string memberName) : Attribute
{
    public string MemberName { get; } = memberName;
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class InstanceMethodDataSourceAttribute(string memberName) : Attribute
{
    public string MemberName { get; } = memberName;
}

[EngineTest(ExpectedResult.Pass)]
public class MethodSourceIdentityTests
{
    public static IEnumerable<int> RealRows() => [42];
    public static IEnumerable<string> UnrelatedRows() => ["unrelated"];
    public IEnumerable<int> InstanceRows() => [42];

    [Test]
    [MethodDataSource(nameof(UnrelatedRows))]
    [InstanceMethodDataSource(nameof(UnrelatedRows))]
    [TUnit.Core.MethodDataSource(nameof(RealRows))]
    public async Task StaticSource<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That((object?)value).IsEqualTo(42);
    }

    [Test]
    [MethodDataSource(nameof(UnrelatedRows))]
    [InstanceMethodDataSource(nameof(UnrelatedRows))]
    [TUnit.Core.InstanceMethodDataSource(nameof(InstanceRows))]
    public async Task InstanceSource<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That((object?)value).IsEqualTo(42);
    }
}
