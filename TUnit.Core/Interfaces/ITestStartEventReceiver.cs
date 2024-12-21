namespace TUnit.Core.Interfaces;

public interface ITestStartEventReceiver : IEventReceiver
{
    ValueTask OnTestStart(BeforeTestContext beforeTestContext);

    void OnTestStartSynchronous(BeforeTestContext beforeTestContext)
#if NET
    {
    // Default implementation that does nothing - Users can override if they wish
    // Synchronous version supports setting AsyncLocal values
    }
#else
    ;
#endif
}