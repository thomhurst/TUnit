namespace TUnit.Core;

public record UntypedTestDetails(ResettableLazy<object> ResettableLazy) : TestDetails
{
    public override object ClassInstance => ResettableLazy.Value;
}