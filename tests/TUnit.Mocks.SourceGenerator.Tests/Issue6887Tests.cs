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

    [Test]
    public async Task Delegate_Parameter_Names_Do_Not_Collide_With_Generated_Locals()
    {
        var source = """
            using System.Threading.Tasks;
            using TUnit.Mocks;

            public delegate Task<int> AsyncHandler(int __result, string __ex, object __rawAsync, object __typedAsync);
            public delegate int SyncHandler(int engine, int del, int mock);

            public class TestUsage
            {
                void M()
                {
                    _ = Mock.OfDelegate<AsyncHandler>();
                    _ = Mock.OfDelegate<SyncHandler>();
                }
            }
            """;

        await Assert.That(GetDelegateFactoryErrors(source)).IsEmpty();
    }

    [Test]
    public async Task User_Defined_Task_Type_Is_Not_Treated_As_Framework_Task()
    {
        var source = """
            using System;
            using TUnit.Mocks;

            namespace Domain
            {
                public sealed class Task<T> { }
                public sealed class ValueTask<T> { }
            }

            public class TestUsage
            {
                void M()
                {
                    _ = Mock.OfDelegate<Func<Domain.Task<int>>>();
                    _ = Mock.OfDelegate<Func<Domain.ValueTask<int>>>();
                }
            }
            """;

        await Assert.That(GetDelegateFactoryErrors(source)).IsEmpty();

        var generated = string.Join("\n", RunGenerator(source));
        await Assert.That(generated).Contains("HandleCallWithReturn<global::Domain.Task<int>>");
        await Assert.That(generated).Contains("HandleCallWithReturn<global::Domain.ValueTask<int>>");
    }

    // The pinned test Roslyn cannot parse the C# 14 extension blocks emitted into the member-surface
    // files, so only errors reported in the delegate factory files are asserted.
    private static string[] GetDelegateFactoryErrors(string source)
        => GetGeneratedCompilationErrors(source)
            .Where(d => d.Location.SourceTree?.FilePath.EndsWith("_MockDelegateFactory.g.cs", StringComparison.Ordinal) == true)
            .Select(d => d.ToString())
            .ToArray();
}
