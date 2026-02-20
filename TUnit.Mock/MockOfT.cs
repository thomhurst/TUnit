namespace TUnit.Mock;

/// <summary>
/// Wraps a generated mock implementation and provides access to Setup, Verify, and Raise surfaces.
/// <para>
/// The source generator produces a derived class per mocked type (e.g. <c>IFoo_Mock : Mock&lt;IFoo&gt;</c>)
/// that shadows Setup, Verify, and Raise with strongly-typed generated surfaces using <c>new</c>.
/// When <c>var</c> resolves to the derived type, the user gets full IntelliSense on setup/verify members.
/// When accessed through the <c>Mock&lt;T&gt;</c> base reference, the <c>dynamic</c> properties
/// still dispatch correctly at runtime.
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
    /// Generated setup surface -- mirrors T's members with <c>Arg{T}</c> parameters.
    /// The generated derived class shadows this with a strongly-typed property.
    /// </summary>
    public dynamic Setup { get; }

    /// <summary>
    /// Generated verification surface -- mirrors T's members with <c>Arg{T}</c> parameters.
    /// The generated derived class shadows this with a strongly-typed property.
    /// </summary>
    public dynamic? Verify { get; internal set; }

    /// <summary>
    /// Generated event-raising surface (if T has events).
    /// The generated derived class shadows this with a strongly-typed property.
    /// </summary>
    public dynamic? Raise { get; internal set; }

    /// <summary>Creates a Mock with a new engine. Used by generated code.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public Mock(T mockObject, object setup, MockBehavior behavior)
    {
        Engine = new MockEngine<T>(behavior);
        Object = mockObject;
        Setup = setup;
    }

    /// <summary>Creates a Mock with an existing engine. Used by generated code.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public Mock(T mockObject, object setup, MockEngine<T> engine)
    {
        Engine = engine;
        Object = mockObject;
        Setup = setup;
    }

    /// <summary>Creates a Mock with an existing engine, setup, and verify surfaces. Used by generated code.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public Mock(T mockObject, object setup, object? verify, MockEngine<T> engine)
    {
        Engine = engine;
        Object = mockObject;
        Setup = setup;
        Verify = verify;
    }

    /// <summary>Creates a Mock with an existing engine, setup, verify, and raise surfaces. Used by generated code.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public Mock(T mockObject, object setup, object? verify, object? raise, MockEngine<T> engine)
    {
        Engine = engine;
        Object = mockObject;
        Setup = setup;
        Verify = verify;
        Raise = raise;
    }

    /// <summary>Clears all setups and call history.</summary>
    public void Reset() => Engine.Reset();

    /// <summary>Implicit conversion to T -- no .Object needed.</summary>
    public static implicit operator T(Mock<T> mock) => mock.Object;
}
