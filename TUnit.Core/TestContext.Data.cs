using System.Collections.Concurrent;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Test data storage for sharing values across hooks and test methods
/// Implements <see cref="ITestData"/> interface
/// </summary>
public partial class TestContext
{
    // Explicit interface implementation for ITestData
    ConcurrentDictionary<string, object?> ITestData.Bag => ObjectBag;
}
