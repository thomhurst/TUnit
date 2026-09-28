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

// An escaped host name must be written back escaped; `partial class event` is invalid C#.
public static partial class @event
{
    [ShouldGeneratePartial(typeof(ParityAssertion))]
    public sealed partial class ParitySource : ShouldSourceBase<int, ParitySource>
    {
        public ParitySource(int value)
            : base(new AssertionContext<int>(value, new StringBuilder("value.Should()")))
        {
        }
    }
}

// A generated partial can't join a file-local type, so no methods are emitted for this wrapper.
// Emitting would declare an unrelated non-file host whose members reference a missing Context.
file static partial class FileLocalWrapperHost
{
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
    public async Task Nested_wrapper_in_escaped_host_gets_generated_methods()
    {
        await new @event.ParitySource(3).BeOddToo();
    }

    [Test]
    public async Task Nested_wrapper_in_file_local_host_gets_no_generated_methods()
    {
        await Assert.That(typeof(FileLocalWrapperHost.ParitySource).GetMethod("BeOddToo")).IsNull();
    }

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
