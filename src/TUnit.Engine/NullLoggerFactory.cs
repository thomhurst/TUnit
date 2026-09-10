using Microsoft.Testing.Platform.Logging;

namespace TUnit.Engine;

/// <summary>
/// Null logger factory implementation for testing
/// </summary>
internal class NullLoggerFactory : ILoggerFactory
{
    public ILogger<T> CreateLogger<T>() => new MtpNullLogger<T>();
    public ILogger CreateLogger(string categoryName) => new MtpNullLogger<object>();
}
