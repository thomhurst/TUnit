using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Mocks.SourceGenerator;
using TUnit.Mocks.SourceGenerator.Builders;
using TUnit.Mocks.SourceGenerator.Models;

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

    // Compare the analysis surface to the actual emitted signatures, rather than maintaining
    // a second list of matcher/predicate/params/any-args overloads. A new conditional shape
    // must be included in analysis too; a new condition requires extending this guard.
    [Test]
    [Arguments(0, false)]
    [Arguments(1, false)]
    [Arguments(4, false)]
    [Arguments(5, false)]
    [Arguments(9, false)]
    [Arguments(1, true)]
    [Arguments(4, true)]
    [Arguments(5, true)]
    public async Task Collision_Analysis_Sees_Every_Emitted_Overload(int parameterCount, bool hasRefStruct)
    {
        var model = new MockTypeModel
        {
            Name = "ISurface",
            FullyQualifiedName = "global::TestNamespace.ISurface<T>",
            Namespace = "TestNamespace",
            IsInterface = true,
            TypeParameters = new(ImmutableArray.Create(new MockTypeParameterModel { Name = "T" }))
        };
        var parameters = Enumerable.Range(0, parameterCount).Select(index => new MockParameterModel
        {
            Name = "value" + index,
            Type = index == 0 && hasRefStruct ? "Span<T>" : "T[]",
            FullyQualifiedType = index == 0 && hasRefStruct ? "global::System.Span<T>" : "T[]",
            IsRefStruct = index == 0 && hasRefStruct,
            SpanElementType = index == 0 && hasRefStruct ? "T" : null,
            ParamsElementType = index == parameterCount - 1 && !(index == 0 && hasRefStruct) ? "T" : null
        }).ToImmutableArray();
        var method = new MockMemberModel
        {
            Name = "Map",
            ReturnType = "U",
            UnwrappedReturnType = "U",
            IsGenericMethod = true,
            TypeParameters = new(ImmutableArray.Create(new MockTypeParameterModel { Name = "U" })),
            Parameters = new(parameters)
        };

        var emitted = new CodeWriter();
        MockMembersBuilder.GenerateClosedReceiverGenericMethodMembers(emitted, method, model, "ISurface");
        var analysis = new CodeWriter();
        MockMembersBuilder.GenerateGenericMethodMembersInCurrentBlock(analysis, method, model, "ISurface", includeAllFrameworkOverloads: true);
        var analysisRoot = CSharpSyntaxTree.ParseText("class Surface { " + analysis + " }").GetRoot();
        await Assert.That(analysisRoot.DescendantTrivia().Any(trivia => trivia.IsDirective)).IsFalse();
        await Assert.That(analysisRoot.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error)).IsFalse();

        var emittedSource = "class Surface { " + emitted + " }";
        var roots = new[]
        {
            CSharpSyntaxTree.ParseText(emittedSource).GetRoot(),
            CSharpSyntaxTree.ParseText(emittedSource, new CSharpParseOptions(preprocessorSymbols: ["NET9_0_OR_GREATER"])).GetRoot()
        };
        var conditions = roots.SelectMany(root => root.DescendantTrivia(descendIntoTrivia: true))
            .Select(trivia => trivia.GetStructure())
            .Select(directive => directive switch
            {
                IfDirectiveTriviaSyntax condition => condition.Condition.ToString(),
                ElifDirectiveTriviaSyntax condition => condition.Condition.ToString(),
                _ => null
            }).Where(condition => condition is not null).Distinct();
        await Assert.That(conditions.All(condition => condition == "NET9_0_OR_GREATER")).IsTrue();

        static IEnumerable<string> Signatures(SyntaxNode root) => root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Select(declaration => declaration.Identifier.ValueText + declaration.TypeParameterList + declaration.ParameterList.NormalizeWhitespace().ToFullString());
        var expected = string.Join("\n", roots.SelectMany(Signatures).Distinct().OrderBy(signature => signature, StringComparer.Ordinal));
        var actual = string.Join("\n", Signatures(analysisRoot).Distinct().OrderBy(signature => signature, StringComparer.Ordinal));
        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    public async Task Nullable_Containers_Are_Kept_And_Nullable_Model_Elements_Are_Skipped()
    {
        var setups = GetSetupsSource("""
            #nullable enable
            namespace TestNamespace
            {
                public interface INullable<T>
                {
                    U List<U>(System.Collections.Generic.List<T>? value);
                    U Array<U>(T[]? value);
                    U Dictionary<U>(System.Collections.Generic.Dictionary<T, U?> value);
                    U Elements<U>(System.Collections.Generic.IEnumerable<T?> value);
                }
                public class Usage
                {
                    void M() => _ = TUnit.Mocks.Mock.Of<INullable<int>>();
                }
            }
            """);
        await Assert.That(setups).IsNotNull();
        await Assert.That(setups!).Contains(" List<U>(");
        await Assert.That(setups).Contains(" Array<U>(");
        await Assert.That(setups).Contains(" Dictionary<U>(");
        await Assert.That(setups).DoesNotContain(" Elements<U>(");
    }

    [Test]
    public async Task Canonical_Method_Type_Parameter_Names_Do_Not_Recurse_Or_Capture()
    {
        var setups = GetSetupsSource("""
            namespace TestNamespace
            {
                public interface ICanonical<T>
                {
                    U Map<U>(U value);
                    __MethodType0 Map<__MethodType0>(int key, __MethodType0 value);
                    U Collapse<U>(T value);
                    __MethodType0 Collapse<__MethodType0>(int value);
                }
                public class Usage
                {
                    void M() => _ = TUnit.Mocks.Mock.Of<ICanonical<int>>();
                }
            }
            """);
        await Assert.That(setups).IsNotNull();
        await Assert.That(setups!).Contains(" Map<U>(");
        await Assert.That(setups).Contains(" Map<__MethodType0>(");
        await Assert.That(setups).DoesNotContain(" Collapse<");
    }

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
