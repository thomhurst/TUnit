using TUnit.Core.Interfaces;

namespace TUnit.Core;

internal record UntypedDiscoveredTest(ResettableLazy<object> ResettableLazy) : DiscoveredTest
{
    public override ValueTask ExecuteTest(CancellationToken cancellationToken)
    {
        return AsyncConvert.ConvertObject(TestDetails.TestMethod.ReflectionInformation.Invoke(ResettableLazy.Value, TestDetails.TestMethodArguments));
    }

    public override ValueTask ResetTestInstance()
    {
        return ResettableLazy.ResetLazy();
    }

    public override IClassConstructor? ClassConstructor
    {
        get;
    } = ResettableLazy.ClassConstructor;
}