namespace TUnit.Mocks.RuntimeStubs;

/// <summary>
/// <see cref="IMock"/> wrapper for a runtime-emitted stub so it can flow through the engine's
/// auto-mock cache. Runtime stubs have no engine: they record no calls and hold no setups, so
/// verification is a no-op and reset only clears remembered property values.
/// </summary>
internal sealed class RuntimeStubMock(RuntimeStub instance) : IMock
{
    public object ObjectInstance { get; } = instance;

    public void VerifyAll()
    {
    }

    public void VerifyNoOtherCalls()
    {
    }

    public void Reset() => ((RuntimeStub)ObjectInstance).ResetState();
}
