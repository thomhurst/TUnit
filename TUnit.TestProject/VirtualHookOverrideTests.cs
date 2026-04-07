namespace TUnit.TestProject;

// Regression test for https://github.com/thomhurst/TUnit/issues/5428
// A virtual [Before(Test)] hook in a base class that is overridden in a derived
// class (also marked with [Before(Test)]) should only execute the override once,
// not twice.
public class VirtualHookOverrideTests
{
    public class BaseTestClass
    {
        public int SetupCallCount;

        [Before(Test)]
        public virtual Task SetupAsync()
        {
            SetupCallCount++;
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

        [Test]
        public void Override_Should_Run_Once()
        {
            if (SetupCallCount != 1)
            {
                throw new Exception($"Expected SetupAsync to run exactly once, but ran {SetupCallCount} times.");
            }
        }
    }
}
