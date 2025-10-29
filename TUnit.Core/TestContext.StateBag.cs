using System.Collections.Concurrent;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Test runtime state storage for sharing values across hooks and test methods
/// Implements <see cref="ITestStateBag"/> interface
/// </summary>
public partial class TestContext
{
    // Explicit interface implementation for ITestStateBag
    ConcurrentDictionary<string, object?> ITestStateBag.Bag => ObjectBag;
}
