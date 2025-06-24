using System.Threading.Tasks;
using TUnit.Core.Contexts;

namespace TUnit.Core.Interfaces;

/// <summary>
/// Simplified interface for test registered event receivers
/// </summary>
public interface ITestRegisteredEventReceiver : IEventReceiver
{
    /// <summary>
    /// Called when a test is registered
    /// </summary>
    ValueTask OnTestRegistered(TestRegisteredContext context);
}