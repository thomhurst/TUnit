namespace TUnit.Core.Interfaces;

/// <summary>
/// Simplified interface for test registered event receivers
/// </summary>
public interface ITestRegisteredEventReceiver : IEventReceiver
{
    /// <summary>
    /// Called when a test is registered
    /// </summary>
    #if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Type comes from runtime objects that cannot be annotated")]
    #endif
    ValueTask OnTestRegistered(TestRegisteredContext context);
}
