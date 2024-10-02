﻿using TUnit.Core.Logging;

namespace TUnit.Core.Helpers;

internal class Disposer
{
    private readonly ILogger _logger;

    public Disposer(ILogger logger)
    {
        _logger = logger;
    }
    
    public async ValueTask DisposeAsync(object? obj)
    {
        try
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
        catch (Exception e)
        {
            if (_logger != null)
            {
                await _logger.LogErrorAsync(e);
            }
        }
    }
}