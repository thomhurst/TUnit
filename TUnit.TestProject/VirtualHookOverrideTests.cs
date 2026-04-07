using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

// Regression test for https://github.com/thomhurst/TUnit/issues/5428
// A virtual [Before(Test)]/[After(Test)] hook in a base class that is overridden
// in a derived class (also marked with the same hook attribute) should only execute
// the override once — not twice — because both registrations would otherwise be
// invoked via virtual dispatch on the same instance.
public class VirtualHookOverrideTests
{
    public class BaseTestClass
    {
        public int SetupCalls;
        public int TeardownCalls;

        [Before(Test)]
        public virtual Task SetupAsync()
        {
            SetupCalls++;
            return Task.CompletedTask;
        }

        [After(Test)]
        public virtual Task TeardownAsync()
        {
            TeardownCalls++;
            return Task.CompletedTask;
        }
    }

    public class DerivedTestClass : BaseTestClass
    {
        [Before(Test)]
        public override async Task SetupAsync()
        {
            await base.SetupAsync();
        }

        [After(Test)]
        public override async Task TeardownAsync()
        {
            await base.TeardownAsync();
        }

        [Test]
        [EngineTest(ExpectedResult.Pass)]
        public async Task Override_Should_Run_Once()
        {
            await Assert.That(SetupCalls).IsEqualTo(1);
        }
    }
}
