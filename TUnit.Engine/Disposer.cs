using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace TUnit.Engine;

internal class Disposer
{
    private readonly IMessageLogger _messageLogger;

    public Disposer(IMessageLogger messageLogger)
    {
        _messageLogger = messageLogger;
    }
    
    public async ValueTask DisposeAsync(object? obj)
    {
        try
        {
            if (obj is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }

            if (obj is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        catch (Exception e)
        {
            _messageLogger.SendMessage(TestMessageLevel.Error, e.ToString());
        }
    }
}