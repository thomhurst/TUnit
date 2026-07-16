using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._5450;

// Regression test for https://github.com/thomhurst/TUnit/issues/5450 (related to #5428).
//
// The original failure: a virtual [Before(Test)] hook on a base class was overridden in an
// abstract intermediate class that *also* declared [Before(Test)] on the override, then concrete
// subclasses inherited the tests via [InheritsTests]. The duplicate attribute caused the override
// to execute twice per test. Fixed at compile time by TUnit0074. This test guards the positive
// shape across every concrete [InheritsTests] subclass and both discovery paths.
public class InheritsTestsVirtualHookOverrideTests
{
    public class BaseTestClass
    {
        public int SetupCalls;
        public int TeardownCalls;

        [Before(Test)]
        public virtual async Task SetupAsync()
        {
            SetupCalls++;
            // Inline assertion — see VirtualHookOverrideTests for the rationale.
            await Assert.That(SetupCalls).IsEqualTo(1);
        }

        [After(Test)]
        public virtual async Task TeardownAsync()
        {
            TeardownCalls++;
            await Assert.That(TeardownCalls).IsEqualTo(1);
        }
    }

    public abstract class IntermediateTestClass : BaseTestClass
    {
        // No [Before(Test)] / [After(Test)] — see the analogous note in VirtualHookOverrideTests.
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

    [InheritsTests]
    public class ConcreteTestClassA : IntermediateTestClass;

    [InheritsTests]
    public class ConcreteTestClassB : IntermediateTestClass;
}
