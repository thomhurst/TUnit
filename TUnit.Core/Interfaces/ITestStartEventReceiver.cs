using System.Threading.Tasks;

namespace TUnit.Core.Interfaces;

/// <summary>
/// Simplified interface for test start event receivers
/// </summary>
public interface ITestStartEventReceiver : IEventReceiver
{
    /// <summary>
    /// Called when a test starts
    /// </summary>
    ValueTask OnTestStart(TestContext context);
}
