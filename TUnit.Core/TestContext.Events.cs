using TUnit.Core.Interfaces;

namespace TUnit.Core;

public partial class TestContext
{
    internal TestContextEvents Events => _testBuilderContext.Events;

    TestContextEvents ITestEvents.Events => Events;
}
