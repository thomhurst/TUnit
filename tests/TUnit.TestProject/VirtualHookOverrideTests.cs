using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

// Regression test for https://github.com/thomhurst/TUnit/issues/5428
// See Bugs/5450 for the [InheritsTests] variant.
public class VirtualHookOverrideTests
{
    public class BaseTestClass
    {
        public int SetupCalls;
        public int TeardownCalls;

        [Before(Test)]
        public virtual async Task SetupAsync()
        {
            SetupCalls++;
            // Inline assertion: a duplicate registration would invoke this twice and fail on the
            // second call. A separate verification hook can't sit after the base hook because
            // After-hooks are sorted derived-class-first across the type hierarchy (Order only
            // sorts within a single type level), so it would run before TeardownAsync below.
            await Assert.That(SetupCalls).IsEqualTo(1);
        }

        [After(Test)]
        public virtual async Task TeardownAsync()
        {
            TeardownCalls++;
            await Assert.That(TeardownCalls).IsEqualTo(1);
        }
    }

    public class DerivedTestClass : BaseTestClass
    {
        // No [Before(Test)] / [After(Test)] here — the base's attributes already register these
        // methods and virtual dispatch routes to the override. TUnit0074 would flag a redundant
        // re-declaration.
        public override async Task SetupAsync()
        {
            await base.SetupAsync();
        }

        public override async Task TeardownAsync()
        {
            await base.TeardownAsync();
        }

        [Test]
        [EngineTest(ExpectedResult.Pass)]
        public async Task Override_Runs_Exactly_Once()
        {
            await Assert.That(SetupCalls).IsEqualTo(1);
        }
    }
}
