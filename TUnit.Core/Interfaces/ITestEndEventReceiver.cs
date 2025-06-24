using System.Threading.Tasks;

namespace TUnit.Core.Interfaces;

/// <summary>
/// Simplified interface for test end event receivers
/// </summary>
public interface ITestEndEventReceiver : IEventReceiver
{
    /// <summary>
    /// Called when a test ends
    /// </summary>
    ValueTask OnTestEnd(TestContext context);
}