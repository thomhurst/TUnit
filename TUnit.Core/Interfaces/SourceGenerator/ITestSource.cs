namespace TUnit.Core.Interfaces.SourceGenerator;

public interface ITestSource
{
    IReadOnlyList<SourceGeneratedTestNode> CollectTests();
}

public interface IStaticHookSource
{
    IReadOnlyList<StaticHookMethod> Tests { get; }
}

// public interface IInstanceHookSource
// {
//     IReadOnlyList<InstanceHookMethod> Tests { get; }
// }