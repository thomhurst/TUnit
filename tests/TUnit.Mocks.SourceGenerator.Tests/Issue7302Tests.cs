namespace TUnit.Mocks.SourceGenerator.Tests;

// Regression: https://github.com/thomhurst/TUnit/issues/7302
// Members of a generic extension block (extension<T>(Mock<IFoo<T>> mock)) do not bind when the call
// passes explicit method type arguments, so Mock.Of<IFoo<int>>().Invoke<string>() failed with CS1061.
// Each closed registration now gets a closed-receiver copy of the generic-method setups. The test
// compilation's Roslyn predates extension blocks, so TUnit.Mocks.Tests compiles and runs them.
public class Issue7302Tests : SnapshotTestBase
{
    private const string Contracts = """
        namespace TestNamespace
        {
            public interface IInvoker<TContext>
            {
                TResult Invoke<TResult>(object message);
                TState RoundTrip<TState>(TContext context, TState state) where TState : struct;
                int Count();
            }

            public interface IConstrained<T>
            {
                TOther Select<TOther>() where TOther : T;
                TOther Pick<TOther>(T? fallback);
                TOther Keep<TOther>(T value);
            }

            public class Outer<TOuter>
            {
                public interface IInner<TInner>
                {
                    TResult Map<TResult>(TOuter left, TInner right);
                }
            }

            public struct Point
            {
                public int X;
            }
        }
        """;

    private const string SetupsFileMarker = "_ClosedMockMemberExtensions_";

    [Test]
    public async Task Overloads_That_Collapse_After_Substitution_Are_Omitted()
    {
        var source = """
            namespace TestNamespace
            {
                public interface IOverloaded<T>
                {
                    TResult Map<TResult>(T value);
                    TOther Map<TOther>(int value);
                    TResult Nested<TResult>(System.Collections.Generic.List<T> value);
                    TOther Nested<TOther>(System.Collections.Generic.List<int> value);
                    TResult Keep<TResult>(T value);
                }
                public class Usage
                {
                    void M() => _ = TUnit.Mocks.Mock.Of<IOverloaded<int>>();
                }
            }
            """;
        var setups = GetSetupsSource(source);
        await Assert.That(setups).IsNotNull();
        await Assert.That(setups!).Contains(" Keep<TResult>(");
        await Assert.That(setups).DoesNotContain(" Map<");
        await Assert.That(setups).DoesNotContain(" Nested<");
    }

    [Test]
    public Task Closed_Generic_Method_Setups_Snapshot()
        => VerifyGeneratorOutput(Usage(
            "TUnit.Mocks.Mock.Of<IInvoker<int>>()",
            "TUnit.Mocks.Mock.Of<IInvoker<string>>()"));

    [Test]
    public async Task Closed_Receiver_Aliases_Every_Type_Argument_In_Order()
    {
        // Containing types' arguments come first, matching the open model's type parameters.
        var setups = GetSetupsSource(Usage("TUnit.Mocks.Mock.Of<Outer<int>.IInner<Point>>()"));

        await Assert.That(setups).IsNotNull();
        await Assert.That(setups!).Contains("using TOuter = int;");
        await Assert.That(setups).Contains("using TInner = global::TestNamespace.Point;");
        await Assert.That(setups).Contains("extension(global::TUnit.Mocks.Mock<global::TestNamespace.Outer<TOuter>.IInner<TInner>> mock)");
        await Assert.That(setups).Contains(" Map<TResult>(");
    }

    [Test]
    public async Task Repeated_Usages_Emit_One_Closed_Receiver()
    {
        var setups = GetSetupsSource(Usage(
            "TUnit.Mocks.Mock.Of<IInvoker<int>>()",
            "TUnit.Mocks.Mock.Of<IInvoker<int>>(TUnit.Mocks.MockBehavior.Strict)"));

        await Assert.That(setups).IsNotNull();
        await Assert.That(setups!).Contains("_ClosedMockMemberExtensions_0");
        await Assert.That(setups).DoesNotContain("_ClosedMockMemberExtensions_1");
    }

    [Test]
    public async Task Methods_Whose_Meaning_An_Alias_Would_Change_Are_Skipped()
    {
        // `where TOther : T` is invalid once T is int, and `T?` would become Nullable<int>.
        var source = Usage("TUnit.Mocks.Mock.Of<IConstrained<int>>()");

        var setups = GetSetupsSource(source);

        await Assert.That(setups).IsNotNull();
        await Assert.That(setups!).Contains(" Keep<TOther>(");
        await Assert.That(setups).DoesNotContain(" Select<TOther>(");
        await Assert.That(setups).DoesNotContain(" Pick<TOther>(");
    }

    [Test]
    public async Task Generic_Interface_Without_Generic_Methods_Emits_No_Setups()
    {
        var source = """
            namespace TestNamespace
            {
                public interface IRepository<T>
                {
                    T Find(int id);
                }

                public class Usage
                {
                    void M() => _ = TUnit.Mocks.Mock.Of<IRepository<int>>();
                }
            }
            """;

        await Assert.That(GetSetupsSource(source)).IsNull();
    }

    [Test]
    public async Task Open_Type_Argument_Emits_No_Setups()
    {
        var source = Contracts + """

            namespace TestNamespace
            {
                public class Usage
                {
                    TUnit.Mocks.Mock<IInvoker<T>> Create<T>() => TUnit.Mocks.Mock.Of<IInvoker<T>>();
                }
            }
            """;

        await Assert.That(GetSetupsSource(source)).IsNull();
    }

    private static string Usage(params string[] expressions)
        => Contracts + $$"""

            namespace TestNamespace
            {
                public class Usage
                {
                    void M()
                    {
                        {{string.Join("\n            ", expressions.Select(e => $"_ = {e};"))}}
                    }
                }
            }
            """;

    private static string? GetSetupsSource(string source)
        => RunGenerator(source).SingleOrDefault(file => file.Contains(SetupsFileMarker, StringComparison.Ordinal));
}
