using System.Text;
using TUnit.Assertions.Core;
using TUnit.Assertions.Should.Attributes;
using TUnit.Assertions.Should.Core;

namespace TUnit.Assertions.Should.Tests;

public sealed class ParityAssertion : Assertion<int>
{
    public ParityAssertion(AssertionContext<int> context) : base(context) { }

    public OddAssertion IsOddToo() => new(Context);

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<int> metadata)
        => Task.FromResult(AssertionResult.Passed);

    protected override string GetExpectation() => "to have a parity";
}

public static partial class NestedWrapperHost<THost>
{
    // The generated partial must land inside NestedWrapperHost<THost>. At namespace scope it
    // would declare an unrelated class without a Context member and fail to compile.
    [ShouldGeneratePartial(typeof(ParityAssertion))]
    public sealed partial class ParitySource : ShouldSourceBase<int, ParitySource>
    {
        public ParitySource(int value)
            : base(new AssertionContext<int>(value, new StringBuilder("value.Should()")))
        {
        }
    }
}

public class NestedWrapperTests
{
    [Test]
    public async Task Nested_wrapper_gets_generated_methods()
    {
        await new NestedWrapperHost<string>.ParitySource(3).BeOddToo();
    }

    [Test]
    public async Task Nested_wrapper_generated_method_fails_for_even_value()
    {
        await Assert.That(async () => await new NestedWrapperHost<string>.ParitySource(4).BeOddToo())
            .Throws<Exception>();
    }
}
