using System.Collections.Concurrent;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public partial class TestContext
{
    ConcurrentDictionary<string, object?> ITestStateBag.Items => ObjectBag;
}
