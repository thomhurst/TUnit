namespace TUnit.Mocks.SourceGenerator.Tests;

// Regression: https://github.com/thomhurst/TUnit/issues/6887
// A delegate returning a non-generic Task/ValueTask is modelled with IsVoid = true, so the delegate
// factory emitted the Action-style lambda body with no return statement (CS1643).
public class Issue6887Tests : SnapshotTestBase
{
    [Test]
    public Task Delegates_Returning_Task_And_ValueTask()
    {
        var source = """
            using System;
            using System.Threading.Tasks;
            using TUnit.Mocks;

            public class TestUsage
            {
                void M()
                {
                    _ = Mock.OfDelegate<Func<int, Task>>();
                    _ = Mock.OfDelegate<Func<int, Task<int>>>();
                    _ = Mock.OfDelegate<Func<ValueTask>>();
                    _ = Mock.OfDelegate<Func<string, ValueTask<string>>>();
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }
}
