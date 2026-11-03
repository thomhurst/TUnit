using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.TestDataAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class GenericTupleArrayMethodDataSourceTests : BaseAnalyzerTests
{
    [Test]
    public async Task ExistingGenericTupleKeepsRuntimeConversions()
    {
        await Verifier.VerifyAnalyzerAsync("""
            using TUnit.Core;
            public class C
            {
                public static (int, string) Row() => (1, "2");
                [Test, MethodDataSource(nameof(Row))]
                public void Test<T>((T, int) row) { }
            }
            """);
    }

    [Test]
    public async Task OpenClassTuplePreservesImplicitArrayCovariance()
    {
        await Verifier.VerifyAnalyzerAsync("""
            #pragma warning disable TUnit0046
            using System.Collections.Generic;
            using TUnit.Core;
            public class C<T>
            {
                public static (int, List<T>[]) Row() => default;
                [Test, MethodDataSource(nameof(Row))]
                public void Test((int, IEnumerable<T>[]) row) { }
            }
            """);
    }

    [Test]
    public async Task SeparateParametersPreserveInferredArrayCovariance()
    {
        await Verifier.VerifyAnalyzerAsync("""
            #pragma warning disable TUnit0046
            using System.Collections.Generic;
            using TUnit.Core;
            public class C
            {
                public static (int, List<int>[], int) Row() => default;
                [Test, MethodDataSource(nameof(Row))]
                public void Test<T>(T seed, IEnumerable<T>[] values, int end) { }
            }
            """);
    }

    [Test]
    [Arguments("string")]
    [Arguments("int")]
    public async Task ContravariantArrayPreservesOpenTypeArgument(string inferredType)
    {
        await Verifier.VerifyAnalyzerAsync($$"""
            #pragma warning disable TUnit0046
            using TUnit.Core;
            public interface IConsumer<in A> { }
            public class C
            {
                public static ({{inferredType}}, IConsumer<{{inferredType}}>[], int) Row() => default;
                [Test, MethodDataSource(nameof(Row))]
                public void Test<T>(T seed, IConsumer<T>[] values, int end) { }
            }
            """);
    }

    [Test]
    public async Task InterfaceAlternativeCanSatisfyLaterParameterBinding()
    {
        await Verifier.VerifyAnalyzerAsync("""
            #pragma warning disable TUnit0046
            using TUnit.Core;
            public interface IInvariant<A, B> { }
            public interface IPair<out A, out B> { }
            public class Box<A> { }
            public class Alternative : IInvariant<int, int>, IInvariant<string, string> { }
            public class C
            {
                public static (string, IPair<Alternative, Box<string>>[], int) Row() => default;
                [Test, MethodDataSource(nameof(Row))]
                public void Test<T>(T seed, IPair<IInvariant<T, T>, Box<T>>[] values, int end) { }
            }
            """);
    }

    [Test]
    public async Task ExplicitDelegateTypeSatisfiesBothContravariantBounds()
    {
        await Verifier.VerifyAnalyzerAsync("""
            #pragma warning disable TUnit0046
            using System;
            using TUnit.Core;
            public interface IContravariant<in A, in B> { }
            public class C
            {
                public static (int, IContravariant<Action<string>, Action<Uri>>[]) Row() => default;
                [Test, MethodDataSource(nameof(Row)), GenerateGenericTest(typeof(Action<object>))]
                public void Test<T>((int, IContravariant<T, T>[]) row) { }
            }
            """);
    }

    [Test]
    [Arguments("(Box<int>[], Box<string>[])", "(Box<T>[], T[])", true)]
    [Arguments("(Box<string>[], Box<int>[])", "(T[], Box<T>[])", true)]
    [Arguments("(Box<int>[], int[])", "(Box<T>[], T[])", false)]
    [Arguments("(string[], Box<object>[])", "(T[], Box<T>[])", false)]
    [Arguments("IContravariant<string[], IDisposable>[]", "IContravariant<T, T>[]", true)]
    [Arguments("IContravariant<IDisposable, string[]>[]", "IContravariant<T, T>[]", true)]
    [Arguments("IContravariant<string[,], IDisposable>[]", "IContravariant<T, T>[]", true)]
    [Arguments("IContravariant<object[], IList<IDisposable>>[]", "IContravariant<T, T>[]", false)]
    [Arguments("IContravariant<IList<IDisposable>, object[]>[]", "IContravariant<T, T>[]", false)]
    [Arguments("IContravariant<string[], IList<IDisposable>>[]", "IContravariant<T, T>[]", true)]
    [Arguments("IContravariant<string[], IEnumerable<object>>[]", "IContravariant<T, T>[]", false)]
    [Arguments("IContravariant<string[], ICloneable>[]", "IContravariant<T, T>[]", false)]
    [Arguments("IContravariant<string[], Base>[]", "IContravariant<T, T>[]", true)]
    [Arguments("IContravariant<Base, string[]>[]", "IContravariant<T, T>[]", true)]
    [Arguments("IContravariant<string[], Array>[]", "IContravariant<T, T>[]", false)]
    [Arguments("IContravariant<object, string[]>[]", "IContravariant<T, T>[]", false)]
    [Arguments("(Box<int>[], Box<string>[])", "(Box<T>[], Box<T>[])", true)]
    [Arguments("(Box<string>[], Box<int>[])", "(Box<T>[], Box<T>[])", true)]
    [Arguments("(Box<int>[], Box<int>[])", "(Box<T>[], Box<T>[])", false)]
    [Arguments("(LaterAlternative[], Box<string>[])", "(IInvariant<T, T>[], Box<T>[])", false)]
    [Arguments("(Box<string>[], LaterAlternative[])", "(Box<T>[], IInvariant<T, T>[])", false)]
    [Arguments("IPair<int[], string[]>[]", "IPair<T[], T[]>[]", true)]
    [Arguments("IPair<string[], int[]>[]", "IPair<T[], T[]>[]", true)]
    [Arguments("IPair<int[], int[]>[]", "IPair<T[], T[]>[]", false)]
    [Arguments("IPair<string[], Uri[]>[]", "IPair<T[], T[]>[]", false)]
    [Arguments("IContravariant<string[], Uri[]>[]", "IContravariant<T, T>[]", true)]
    [Arguments("IContravariant<Uri[], string[]>[]", "IContravariant<T, T>[]", true)]
    [Arguments("IContravariant<string[][], Uri[][]>[]", "IContravariant<T, T>[]", true)]
    [Arguments("IContravariant<int[], string[]>[]", "IContravariant<T, T>[]", true)]
    [Arguments("IContravariant<Action<string>, Action<Uri>>[]", "IContravariant<T, T>[]", false)]
    [Arguments("IContravariant<Action<string>[], Action<Uri>[]>[]", "IContravariant<T, T>[]", false)]
    [Arguments("IContravariant<string, Uri>[]", "IContravariant<T, T>[]", true)]
    [Arguments("IContravariant<Uri, string>[]", "IContravariant<T, T>[]", true)]
    [Arguments("IContravariant<Base, OtherBase>[]", "IContravariant<T, T>[]", true)]
    [Arguments("IContravariant<Base, Derived>[]", "IContravariant<T, T>[]", false)]
    [Arguments("IContravariant<string, string>[]", "IContravariant<T, T>[]", false)]
    [Arguments("IContravariant<string, ICloneable>[]", "IContravariant<T, T>[]", false)]
    [Arguments("IContravariant<string, IDisposable>[]", "IContravariant<T, T>[]", true)]
    [Arguments("IContravariant<Base, IDisposable>[]", "IContravariant<T, T>[]", false)]
    [Arguments("IContravariant<ICloneable, IDisposable>[]", "IContravariant<T, T>[]", false)]
    [Arguments("IMixed<object, string>[]", "IMixed<T, T>[]", true)]
    [Arguments("IMixed<string, object>[]", "IMixed<T, T>[]", false)]
    [Arguments("IMixed<string, string>[]", "IMixed<T, T>[]", false)]
    [Arguments("IReverse<string, object>[]", "IReverse<T, T>[]", true)]
    [Arguments("IReverse<object, string>[]", "IReverse<T, T>[]", false)]
    [Arguments("IPair<LaterAlternative, Box<string>>[]", "IPair<IInvariant<T, T>, Box<T>>[]", false)]
    [Arguments("IPair<Box<string>, LaterAlternative>[]", "IPair<Box<T>, IInvariant<T, T>>[]", false)]
    [Arguments("IPair<LaterAlternative, Box<object>>[]", "IPair<IInvariant<T, T>, Box<T>>[]", true)]
    [Arguments("int[][]", "IEnumerable<T>[]", false)]
    [Arguments("IEnumerable<int[]>[]", "IEnumerable<IEnumerable<T>>[]", false)]
    [Arguments("IPair<string, Box<int>>[]", "IPair<T, Box<T>>[]", true)]
    [Arguments("IPair<Box<int>, string>[]", "IPair<Box<T>, T>[]", true)]
    [Arguments("IPair<string, Box<object>>[]", "IPair<T, Box<T>>[]", false)]
    [Arguments("IPair<Box<object>, string>[]", "IPair<Box<T>, T>[]", false)]
    [Arguments("IConsumer<Box<string>, object>[]", "IConsumer<Box<T>, T>[]", false)]
    [Arguments("IConsumer<Box<object>, string>[]", "IConsumer<Box<T>, T>[]", true)]
    [Arguments("Alternative[]", "IInvariant<T, T>[]", false)]
    [Arguments("Outer<string>.Inner<int>[]", "Outer<T>.Inner<T>[]", true)]
    [Arguments("Outer<int>.Inner<int>[]", "Outer<T>.Inner<T>[]", false)]
    [Arguments("Pair<int, string>[]", "Pair<T, T>[]", true)]
    [Arguments("Pair<int, int>[]", "Pair<T, T>[]", false)]
    [Arguments("Pair<int[], string[]>[]", "Pair<T[], T[]>[]", true)]
    [Arguments("Pair<int[], int[]>[]", "Pair<T[], T[]>[]", false)]
    [Arguments("Outer<string>.Inner<int>[]", "Outer<object>.Inner<T>[]", true)]
    [Arguments("Outer<string>.Inner<int>[]", "Outer<string>.Inner<T>[]", false)]
    [Arguments("IPair<int, string[][]>[]", "IPair<T, object[][]>[]", false)]
    [Arguments("IConsumer<int, object[][]>[]", "IConsumer<T, string[][]>[]", false)]
    [Arguments("IConsumer<int, string[][]>[]", "IConsumer<T, object[][]>[]", true)]
    [Arguments("IPair<int, int>[]", "IPair<T, object>[]", true)]
    [Arguments("int[]", "T[]", false)]
    [Arguments("int[][]", "T[][]", false)]
    [Arguments("int[,]", "T[,]", false)]
    [Arguments("int[]", "T[,]", true)]
    [Arguments("int", "T[]", true)]
    [Arguments("int[]", "string[]", true)]
    [Arguments("(int, int)[]", "(T, long)[]", true)]
    [Arguments("(int, int)[]", "(T, int)[]", false)]
    [Arguments("(int, string[])[]", "(T, object[])[]", true)]
    [Arguments("(int, string[][])[]", "(T, object[][])[]", true)]
    [Arguments("(int, string[][])[]", "(T, string[][])[]", false)]
    [Arguments("(int, string[][])", "(T, object[][])", false)]
    public async Task TupleArrayElements(string sourceElement, string targetElement, bool expectDiagnostic)
    {
        var attribute = expectDiagnostic ? "{|#0:MethodDataSource(nameof(Row))|}" : "MethodDataSource(nameof(Row))";
        var source = $$"""
            #pragma warning disable TUnit0046 // Mutable array rows are intentional in these binding tests.
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using TUnit.Core;
            using TUnit.Core.Interfaces;
            public interface IContravariant<in A, in B> { }
            public class Base { }
            public class Derived : Base { }
            public class OtherBase { }
            public interface IMixed<out A, in B> { }
            public interface IReverse<in A, out B> { }
            public class LaterAlternative : IInvariant<int, int>, IInvariant<string, string> { }
            public interface IPair<out A, out B> { }
            public interface IConsumer<A, in B> { }
            public interface IInvariant<A, B> { }
            public class Alternative : IInvariant<int, string>, IInvariant<string, string> { }
            public class Pair<A, B> { }
            public class Box<A> { }
            public class Outer<A> { public class Inner<B> { } }
            public class C
            {
                public static (Fixture, CancellationToken, {{sourceElement}}) Row() => default;
                [Test, {{attribute}}]
                public void Test<T>((Fixture, CancellationToken, {{targetElement}}) row) { }
                public struct Fixture : IAsyncInitializer
                {
                    public Task InitializeAsync() => Task.CompletedTask;
                }
            }
            """;

        if (expectDiagnostic)
        {
            // Diagnostics display the nested type name without its containing construction.
            var displayedSource = sourceElement.Replace("Outer<string>.", "").Replace("Outer<int>.", "");
            var displayedTarget = targetElement.Replace("Outer<object>.", "").Replace("Outer<string>.", "").Replace("Outer<T>.", "");
            await Verifier.VerifyAnalyzerAsync(source,
                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData).WithLocation(0)
                    .WithArguments($"Fixture, CancellationToken, {displayedSource}", $"(Fixture, CancellationToken, {displayedTarget})"));
        }
        else
        {
            await Verifier.VerifyAnalyzerAsync(source);
        }
    }
}
