using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public class ResettableLazy<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    TClassConstructor,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    T> : ResettableLazy<T>
    where TClassConstructor : IClassConstructor, new()
    where T : class
{
    public ResettableLazy(string sessionId) : base(new TClassConstructor(), sessionId)
    {
    }
    
    public override async Task ResetLazy()
    {
        await DisposeAsync(ClassConstructor);
        ClassConstructor = new TClassConstructor();
        _factory = () => ClassConstructor.Create<T>(new ClassConstructorMetadata
        {
            TestSessionId = SessionId
        });
        await base.ResetLazy();
    }
}

public class ResettableLazy<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : IAsyncDisposable where T : class
{
    public IClassConstructor? ClassConstructor { get; protected set; }
    
    private Lazy<T> _lazy;
    protected Func<T> _factory;
    
    protected readonly string SessionId;

    protected ResettableLazy(IClassConstructor classConstructor, string sessionId)
    {
        SessionId = sessionId;
        ClassConstructor = classConstructor;
        _factory = () => classConstructor.Create<T>(new ClassConstructorMetadata
        {
            TestSessionId = sessionId
        });
        _lazy = new Lazy<T>(_factory);
    }

    public ResettableLazy(Func<T> factory, string sessionId)
    {
        _factory = factory;
        SessionId = sessionId;
        _lazy = new Lazy<T>(factory);
    }

    public T Value => _lazy.Value;

    public virtual async Task ResetLazy()
    {
        await DisposeAsync();
        
        _lazy = new Lazy<T>(_factory);
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_lazy.IsValueCreated)
        {
            await DisposeAsync(_lazy.Value);
        }
    }

    protected static async ValueTask DisposeAsync(object? obj)
    {
        if (obj is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (obj is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public ResettableLazy<T> Clone()
    {
        return new ResettableLazy<T>(_factory, SessionId);
    }
}