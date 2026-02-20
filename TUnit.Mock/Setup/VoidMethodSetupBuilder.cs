using System.ComponentModel;
using TUnit.Mock.Setup.Behaviors;

namespace TUnit.Mock.Setup;

/// <summary>
/// Concrete implementation of setup builder for void methods.
/// Public for generated code access. Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class VoidMethodSetupBuilder : IVoidMethodSetup, IVoidSetupChain
{
    private readonly MethodSetup _setup;

    public VoidMethodSetupBuilder(MethodSetup setup)
    {
        _setup = setup;
    }

    public IVoidSetupChain Throws<TException>() where TException : Exception, new()
    {
        _setup.AddBehavior(new ThrowBehavior(new TException()));
        return this;
    }

    public IVoidSetupChain Throws(Exception exception)
    {
        _setup.AddBehavior(new ThrowBehavior(exception));
        return this;
    }

    public IVoidSetupChain Callback(Action callback)
    {
        _setup.AddBehavior(new CallbackBehavior(callback));
        return this;
    }

    public IVoidMethodSetup Then() => this;
}
