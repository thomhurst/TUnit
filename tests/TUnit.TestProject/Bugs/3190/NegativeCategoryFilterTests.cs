using System;
using System.Threading.Tasks;

namespace TUnit.TestProject.Bugs._3190;

// This file replicates the issue from GitHub issue #3190:
// When ANY test has [Explicit], negative category filters stop working correctly.
// Expected: /*/*/*/*[Category!=Performance] should exclude all Performance tests
// Actual bug: It runs all non-explicit tests INCLUDING those with Performance category
//
// ROOT CAUSE ANALYSIS:
// The filter evaluation logic is incorrectly handling [Explicit] tests. The presence of
// explicit tests is somehow interfering with negative category filter evaluation.
//
// CORRECT DESIGN PRINCIPLE (Two-Stage Filtering):
//
// Stage 1: Pre-Filter for [Explicit]
//   Create initial candidate list:
//   - START WITH: All non-explicit tests
//   - ADD explicit tests ONLY IF: They are positively and specifically selected
//     ✓ Specific name match: /*/MyExplicitTest
//     ✓ Positive property: /*/*/*/*[Category=Nightly]
//     ✗ Wildcard: /*/*/*/*  (too broad - not a specific selection)
//     ✗ Negative filter: /*/*/*/*[Category!=Performance]  (not a positive selection)
//
// Stage 2: Main Filter
//   Apply the full filter logic (including negations) to the candidate list from Stage 1.
//
// WHY THIS IS CORRECT:
// - [Explicit] means "opt-in only" - never run unless specifically requested
// - Test behavior should be local to the test itself, not dependent on sibling tests
// - Aligns with industry standards (NUnit, etc.)
// - Prevents "last non-explicit test" disaster scenario where deleting one test
//   changes the behavior of 99 unrelated explicit tests
//
// EXPECTED BEHAVIOR FOR THIS TEST:
// Filter: /*/*/*/*[Category!=Performance]
//
// Stage 1 Result (candidate list):
//   - TestClass1.TestMethod1 ✓ (not explicit)
//   - TestClass1.TestMethod2 ✓ (not explicit)
//   - TestClass2.TestMethod1 ✓ (not explicit)
//   - TestClass2.TestMethod2 ✗ (explicit - wildcard doesn't positively select it)
//   - TestClass3.RegularTestWithoutCategory ✓ (not explicit)
//
// Stage 2 Result (after applying [Category!=Performance]):
//   - TestClass1.TestMethod1 ✗ (has Performance category)
//   - TestClass1.TestMethod2 ✓ (no Performance category) ← SHOULD RUN
//   - TestClass2.TestMethod1 ✗ (has Performance category)
//   - TestClass3.RegularTestWithoutCategory ✓ (no Performance category) ← SHOULD RUN
//
// FINAL: 2 tests should run

public class TestClass1
{
    [Test]
    [Category("Performance")]
    public Task TestMethod1()
    {
        // This test has Performance category
        // With filter [Category!=Performance], this should be EXCLUDED
        Console.WriteLine("TestClass1.TestMethod1 executed (has Performance category)");
        return Task.CompletedTask;
    }

    [Test]
    [Property("CI", "false")]
    public Task TestMethod2()
    {
        // This test has CI property but NOT Performance category
        // With filter [Category!=Performance], this should be INCLUDED
        Console.WriteLine("TestClass1.TestMethod2 executed (no Performance category)");
        return Task.CompletedTask;
    }
}

public class TestClass2
{
    [Test]
    [Category("Performance")]
    [Property("CI", "true")]
    public Task TestMethod1()
    {
        // This test has BOTH Performance category and CI property
        // With filter [Category!=Performance], this should be EXCLUDED
        Console.WriteLine("TestClass2.TestMethod1 executed (has Performance category)");
        return Task.CompletedTask;
    }

    [Test]
    [Explicit]
    public Task TestMethod2()
    {
        // This test is marked Explicit - the trigger for the bug
        // With any wildcard filter, this should NOT run unless explicitly requested
        // But its presence causes negative category filters to malfunction
        Console.WriteLine("TestClass2.TestMethod2 executed (Explicit test - should not run with wildcard filter!)");
        throw new NotImplementedException("Explicit test should not run with wildcard filter!");
    }
}

public class TestClass3
{
    [Test]
    public Task RegularTestWithoutCategory()
    {
        // This test has no Performance category and is not Explicit
        // With filter [Category!=Performance], this should be INCLUDED
        Console.WriteLine("TestClass3.RegularTestWithoutCategory executed (no Performance category)");
        return Task.CompletedTask;
    }
}
