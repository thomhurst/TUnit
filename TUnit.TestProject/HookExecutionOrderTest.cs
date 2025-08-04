using System.Collections.Generic;
using TUnit.Core;

namespace TUnit.TestProject;

public class HookExecutionOrderTest
{
    private static readonly List<string> ExecutionOrder = new();

    public class BaseTest
    {
        [Before(Test, Order = 10)] // High order, but should still run first due to hierarchy
        public void BaseBeforeTest()
        {
            // Clear on the very first before hook to handle multiple test runs
            if (ExecutionOrder.Count > 0 && ExecutionOrder[^1].StartsWith("BaseAfterTest"))
            {
                ExecutionOrder.Clear();
            }
            ExecutionOrder.Add("BaseBeforeTest");
        }

        [Before(Test, Order = -5)] // Negative order, should run before BaseBeforeTest at same level
        public void BaseBeforeTest2()
        {
            ExecutionOrder.Add("BaseBeforeTest2");
        }

        [After(Test, Order = -10)] // Negative order, but should still run last due to hierarchy
        public void BaseAfterTest()
        {
            ExecutionOrder.Add("BaseAfterTest");
        }

        [After(Test, Order = 5)] // Higher order, should run after BaseAfterTest at same level
        public void BaseAfterTest2()
        {
            ExecutionOrder.Add("BaseAfterTest2");
        }
    }

    public class MiddleTest : BaseTest
    {
        [Before(Test, Order = 100)] // Very high order, but still runs after base
        public void MiddleBeforeTest()
        {
            ExecutionOrder.Add("MiddleBeforeTest");
        }

        [Before(Test, Order = 0)] // Default order
        public void MiddleBeforeTest2()
        {
            ExecutionOrder.Add("MiddleBeforeTest2");
        }

        [After(Test, Order = -100)] // Very low order, but still runs before base
        public void MiddleAfterTest()
        {
            ExecutionOrder.Add("MiddleAfterTest");
        }

        [After(Test, Order = 0)] // Default order
        public void MiddleAfterTest2()
        {
            ExecutionOrder.Add("MiddleAfterTest2");
        }
    }

    public class DerivedTest : MiddleTest
    {
        [Before(Test, Order = -1000)] // Extremely low order, but still runs after middle
        public void DerivedBeforeTest()
        {
            ExecutionOrder.Add("DerivedBeforeTest");
        }

        [Before(Test, Order = 3)] // Positive order
        public void DerivedBeforeTest2()
        {
            ExecutionOrder.Add("DerivedBeforeTest2");
        }

        [After(Test, Order = 1000)] // Extremely high order, but still runs before middle
        public void DerivedAfterTest()
        {
            ExecutionOrder.Add("DerivedAfterTest");
        }

        [After(Test, Order = -3)] // Negative order
        public void DerivedAfterTest2()
        {
            ExecutionOrder.Add("DerivedAfterTest2");
        }

        [Test]
        public void TestHookExecutionOrder()
        {
            ExecutionOrder.Add("TestMethod");
        }

        [After(Test, Order = 2000)] // Use very high order to run after all other after hooks
        public void VerifyExecutionOrder()
        {
            // Expected order:
            // Before hooks (base to derived, with Order respected within each level):
            // - Base: BaseBeforeTest2 (-5), then BaseBeforeTest (10)
            // - Middle: MiddleBeforeTest2 (0), then MiddleBeforeTest (100)
            // - Derived: DerivedBeforeTest (-1000), then DerivedBeforeTest2 (3)
            // Test method
            // After hooks (derived to base, with Order respected within each level):
            // - Derived: DerivedAfterTest2 (-3), then DerivedAfterTest (1000)
            // - Middle: MiddleAfterTest (-100), then MiddleAfterTest2 (0)
            // - Base: BaseAfterTest (-10), then BaseAfterTest2 (5)

            var expected = new List<string>
            {
                // Before hooks - base to derived
                "BaseBeforeTest2",      // Base level, Order = -5
                "BaseBeforeTest",       // Base level, Order = 10
                "MiddleBeforeTest2",    // Middle level, Order = 0
                "MiddleBeforeTest",     // Middle level, Order = 100
                "DerivedBeforeTest",    // Derived level, Order = -1000
                "DerivedBeforeTest2",   // Derived level, Order = 3

                "TestMethod",

                // After hooks - derived to base
                "DerivedAfterTest2",    // Derived level, Order = -3
                "DerivedAfterTest",     // Derived level, Order = 1000
                "MiddleAfterTest",      // Middle level, Order = -100
                "MiddleAfterTest2",     // Middle level, Order = 0
                "BaseAfterTest",        // Base level, Order = -10
                "BaseAfterTest2"        // Base level, Order = 5
            };

            for (int i = 0; i < expected.Count; i++)
            {
                if (i >= ExecutionOrder.Count || ExecutionOrder[i] != expected[i])
                {
                    throw new Exception($"Hook execution order is incorrect at index {i}. Expected: {expected[i]}, Actual: {(i < ExecutionOrder.Count ? ExecutionOrder[i] : "missing")}. Full order - Expected: [{string.Join(", ", expected)}], Actual: [{string.Join(", ", ExecutionOrder)}]");
                }
            }
        }
    }
}
