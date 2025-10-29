using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Test events integration
/// Implements <see cref="ITestEvents"/> interface
/// </summary>
public partial class TestContext
{
    // Explicit interface implementation for ITestEvents
    TestContextEvents ITestEvents.Events => Events;
}
