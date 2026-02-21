namespace TUnit.Mock;

/// <summary>
/// Wraps a generated mock implementation and provides access to Setup, Verify, and Raise surfaces.
/// <para>
/// The source generator produces extension methods on <c>IMockSetup&lt;T&gt;</c>, <c>IMockVerify&lt;T&gt;</c>,
/// and <c>IMockRaise&lt;T&gt;</c> that provide strongly-typed setup/verify/raise members.
/// This approach is fully AOT-compatible — no dynamic dispatch is needed.
/// </para>
/// </summary>
public class Mock<T> where T : class
{
    /// <summary>The mock engine. Used by generated code. Not intended for direct use.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public MockEngine<T> Engine { get; }

    /// <summary>The mock object that implements T.</summary>
    public T Object { get; }

    /// <summary>The mock behavior (Loose or Strict).</summary>
    public MockBehavior Behavior => Engine.Behavior;

    /// <summary>
    /// Generated setup surface -- extension methods provide T's members with <c>Arg{T}</c> parameters.
    /// </summary>
    public IMockSetup<T> Setup { get; }

    /// <summary>
    /// Generated verification surface -- extension methods provide T's members with <c>Arg{T}</c> parameters.
    /// </summary>
    public IMockVerify<T>? Verify { get; internal set; }

    /// <summary>
    /// Generated event-raising surface (if T has events).
    /// </summary>
    public IMockRaise<T>? Raise { get; internal set; }

    /// <summary>Creates a Mock with a new engine. Used by generated code.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public Mock(T mockObject, object setup, MockBehavior behavior)
    {
        Engine = new MockEngine<T>(behavior);
        Object = mockObject;
        Setup = (IMockSetup<T>)setup;
    }

    /// <summary>Creates a Mock with an existing engine. Used by generated code.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public Mock(T mockObject, object setup, MockEngine<T> engine)
    {
        Engine = engine;
        Object = mockObject;
        Setup = (IMockSetup<T>)setup;
    }

    /// <summary>Creates a Mock with an existing engine, setup, and verify surfaces. Used by generated code.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public Mock(T mockObject, object setup, object? verify, MockEngine<T> engine)
    {
        Engine = engine;
        Object = mockObject;
        Setup = (IMockSetup<T>)setup;
        Verify = (IMockVerify<T>?)verify;
    }

    /// <summary>Creates a Mock with an existing engine, setup, verify, and raise surfaces. Used by generated code.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public Mock(T mockObject, object setup, object? verify, object? raise, MockEngine<T> engine)
    {
        Engine = engine;
        Object = mockObject;
        Setup = (IMockSetup<T>)setup;
        Verify = (IMockVerify<T>?)verify;
        Raise = (IMockRaise<T>?)raise;
    }

    /// <summary>Clears all setups and call history.</summary>
    public void Reset() => Engine.Reset();

    /// <summary>Implicit conversion to T -- no .Object needed.</summary>
    public static implicit operator T(Mock<T> mock) => mock.Object;
}
