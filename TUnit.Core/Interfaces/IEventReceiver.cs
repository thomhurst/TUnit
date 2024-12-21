namespace TUnit.Core.Interfaces;

public interface IEventReceiver
{
#if NET
    public int Order => 0;
#else
    public int Order { get; }
#endif
}