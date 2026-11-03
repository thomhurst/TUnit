using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.TestDataAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class ReferenceTupleMethodDataSourceTests : BaseAnalyzerTests
{
    // Runtime binding coverage: StaticTupleDataSourceTests in TUnit.TestProject,
    // executed in both generated and reflection modes.
    [Test]
    public async Task Method()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using System;
            using System.Collections.Generic;
            using TUnit.Core;
            public class C { [Test] [MethodDataSource(nameof(Rows))] public void Test(int value, string label) {}
                public static IEnumerable<Tuple<int, string>> Rows() => [ Tuple.Create(5, "b") ];
            }
            """);
    }

    [Test]
    public async Task GenericClass()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using System;
            using System.Collections.Generic;
            using TUnit.Core;
            [GenerateGenericTest(typeof(int))] [MethodDataSource(nameof(Rows))] public class C<T>(T value, string label) { [Test] public void Test() {}
                public static IEnumerable<Tuple<int, string>> Rows() => [ Tuple.Create(5, "b") ];
            }
            """);
    }

    [Test]
    public async Task WholeTuple()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using System;
            using System.Collections.Generic;
            using TUnit.Core;
            public class C { [Test] [MethodDataSource(nameof(Rows))] public void Test(Tuple<int, string> row) {}
                public static IEnumerable<Tuple<int, string>> Rows() => [ Tuple.Create(5, "b") ];
            }
            """);
    }

    [Test]
    public async Task LongTuple()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using System;
            using System.Collections.Generic;
            using TUnit.Core;
            public class C { [Test] [MethodDataSource(nameof(Rows))] public void Test(int a, int b, int c, int d, int e, int f, int g, int h, string i) {}
                public static IEnumerable<Tuple<int, int, int, int, int, int, int, Tuple<int, string>>> Rows() => [ new(1, 2, 3, 4, 5, 6, 7, Tuple.Create(8, "b")) ];
            }
            """);
    }

    [Test]
    public async Task MismatchedElementIsReported()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using System;
            using System.Collections.Generic;
            using TUnit.Core;
            public class C
            {
                [Test]
                [{|#0:MethodDataSource(nameof(Rows))|}]
                public void Test(int value, int label) {}
                public static IEnumerable<Tuple<int, string>> Rows() => [Tuple.Create(5, "b")];
            }
            """,
            Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                .WithLocation(0).WithArguments("int, string", "int, int"));
    }

    [Test]
    [Arguments("Tuple<int, int>", "Tuple<long, long>", "int, int")]
    [Arguments("Tuple<int>", "Tuple<long>", "int")]
    public async Task ConstructorTupleWideningIsReported(string sourceType, string targetType, string sourceElements)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            using System;
            using System.Collections.Generic;
            using TUnit.Core;
            [{|#0:MethodDataSource(nameof(Row))|}]
            public class Direct({{targetType}} row)
            {
                [Test] public void Test() {}
                public static {{sourceType}} Row() => default!;
            }
            [{|#1:MethodDataSource(nameof(Rows))|}]
            public class Enumerable({{targetType}} row)
            {
                [Test] public void Test() {}
                public static IEnumerable<{{sourceType}}> Rows() => [];
            }
            """,
            Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                .WithLocation(0).WithArguments(sourceElements, targetType),
            Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                .WithLocation(1).WithArguments(sourceElements, targetType));
    }

    [Test]
    public async Task ConstructorWholeTupleWithExactType()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using System;
            using System.Collections.Generic;
            using TUnit.Core;
            [MethodDataSource(nameof(Row))]
            public class Direct(Tuple<int, int> row)
            {
                [Test] public void Test() {}
                public static Tuple<int, int> Row() => default!;
            }
            [MethodDataSource(nameof(Rows))]
            public class Enumerable(Tuple<int, int> row)
            {
                [Test] public void Test() {}
                public static IEnumerable<Tuple<int, int>> Rows() => [];
            }
            """);
    }

    [Test]
    [Arguments("Tuple<(int, int)>", "Tuple<(int, int)?>")]
    [Arguments("Tuple<int>", "Tuple<long?>")]
    [Arguments("Tuple<char>", "Tuple<decimal>")]
    [Arguments("Tuple<string>", "Tuple<object>")]
    [Arguments("Tuple<int>", "Tuple<object>")]
    [Arguments("Tuple<int>", "Tuple<long>")]
    [Arguments("Tuple<int, int>", "Tuple<long, long>")]
    [Arguments("Tuple<Tuple<int>, int>", "Tuple<Tuple<int>, long>")]
    [Arguments("Tuple<int, int, int, int, int, int, int, Tuple<int>>", "Tuple<long, long, long, long, long, long, long, Tuple<long>>")]
    [Arguments("Tuple<int, int, int, int, int, int, int, Tuple<int, int, int, int, int, int, int, Tuple<int>>>", "Tuple<long, long, long, long, long, long, long, Tuple<long, long, long, long, long, long, long, Tuple<long>>>")]
    public async Task WholeTupleWithNumericWidening(string sourceType, string targetType)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            using System;
            using System.Collections.Generic;
            using TUnit.Core;
            public class C
            {
                [Test] [MethodDataSource(nameof(Row))] public void Direct({{targetType}} row) {}
                [Test] [MethodDataSource(nameof(Rows))] public void Enumerable({{targetType}} row) {}
                public static {{sourceType}} Row() => default!;
                public static IEnumerable<{{sourceType}}> Rows() => [];
            }
            """);
    }

    [Test]
    [Arguments("Tuple<nint>", "Tuple<decimal>", "nint")]
    [Arguments("Tuple<nuint>", "Tuple<decimal>", "nuint")]
    [Arguments("Tuple<nint>", "Tuple<decimal?>", "nint")]
    [Arguments("Tuple<nuint>", "Tuple<decimal?>", "nuint")]
    [Arguments("Tuple<nint?>", "Tuple<decimal?>", "nint?")]
    [Arguments("Tuple<nuint?>", "Tuple<decimal?>", "nuint?")]
    [Arguments("Tuple<(nint, int)>", "Tuple<(decimal, long)>", "(nint, int)")]
    [Arguments("Tuple<nint>", "Tuple<double>", "nint")]
    [Arguments("Tuple<nint>", "Tuple<float>", "nint")]
    [Arguments("Tuple<nuint>", "Tuple<double>", "nuint")]
    [Arguments("Tuple<nuint>", "Tuple<float>", "nuint")]
    [Arguments("Tuple<nint?>", "Tuple<double?>", "nint?")]
    [Arguments("Tuple<nuint>", "Tuple<float?>", "nuint")]
    [Arguments("Tuple<(nint, int)>", "Tuple<(double, long)>", "(nint, int)")]
    [Arguments("Tuple<char>", "Tuple<float>", "char")]
    [Arguments("Tuple<char>", "Tuple<double>", "char")]
    [Arguments("Tuple<char?>", "Tuple<float?>", "char?")]
    [Arguments("Tuple<char>", "Tuple<double?>", "char")]
    [Arguments("Tuple<(char, int)>", "Tuple<(float, long)>", "(char, int)")]
    [Arguments("Tuple<(char, int)>", "Tuple<(double, long)>", "(char, int)")]
    [Arguments("Tuple<((int, int), int)>", "Tuple<((long, long)?, long)>", "((int, int), int)")]
    [Arguments("Tuple<(int, int)>", "Tuple<(long, long)?>", "(int, int)")]
    [Arguments("Tuple<Tuple<int, int>>", "Tuple<long, long>", "Tuple<int, int>")]
    [Arguments("Tuple<Tuple<int>>", "Tuple<long>", "Tuple<int>")]
    [Arguments("Tuple<Tuple<int>, int>", "Tuple<Tuple<long>, long>", "Tuple<int>, int")]
    [Arguments("Tuple<long, long>", "Tuple<int, int>", "long, long")]
    [Arguments("Tuple<int, string>", "Tuple<long, long>", "int, string")]
    [Arguments("Tuple<int, int>", "Tuple<long, long, long>", "int, int")]
    [Arguments("Tuple<int, int, int>", "Tuple<long, long>", "int, int, int")]
    [Arguments("Tuple<int, int, int, int, int, int, int, Tuple<string>>", "Tuple<long, long, long, long, long, long, long, Tuple<long>>", "int, int, int, int, int, int, int, string")]
    public async Task IncompatibleWholeTupleIsReported(string sourceType, string targetType, string sourceElements)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            using System;
            using System.Collections.Generic;
            using TUnit.Core;
            public class C
            {
                [Test] [{|#0:MethodDataSource(nameof(Row))|}] public void Test({{targetType}} row) {}
                [Test] [{|#1:MethodDataSource(nameof(Rows))|}] public void Enumerable({{targetType}} row) {}
                public static {{sourceType}} Row() => default!;
                public static IEnumerable<{{sourceType}}> Rows() => [];
            }
            """,
            Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                .WithLocation(0).WithArguments(sourceElements, targetType),
            Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                .WithLocation(1).WithArguments(sourceElements, targetType));
    }

    [Test]
    public async Task ExactUserDefinedConversionToNullableTuple()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using System;
            using System.Collections.Generic;
            using TUnit.Core;
            public class Source { public static implicit operator (int, int)(Source value) => (1, 2); }
            public class C
            {
                [Test] [MethodDataSource(nameof(Row))] public void Direct(Tuple<(int, int)?> row) {}
                [Test] [MethodDataSource(nameof(Rows))] public void Enumerable(Tuple<(int, int)?> row) {}
                public static Tuple<Source> Row() => Tuple.Create(new Source());
                public static IEnumerable<Tuple<Source>> Rows() => [Row()];
            }
            """);
    }

    [Test]
    public async Task ComposedUserDefinedConversionIsReported()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using System;
            using System.Collections.Generic;
            using TUnit.Core;
            public class Base {}
            public class Derived : Base {}
            public class Source { public static implicit operator Derived(Source value) => new(); }
            public class C
            {
                [Test] [{|#0:MethodDataSource(nameof(Row))|}] public void Direct(Tuple<Base> row) {}
                [Test] [{|#1:MethodDataSource(nameof(Rows))|}] public void Enumerable(Tuple<Base> row) {}
                [Test] [MethodDataSource(nameof(Row))] public void Exact(Tuple<Derived> row) {}
                public static Tuple<Source> Row() => Tuple.Create(new Source());
                public static IEnumerable<Tuple<Source>> Rows() => [Row()];
            }
            """,
            Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                .WithLocation(0).WithArguments("Source", "Tuple<Base>"),
            Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                .WithLocation(1).WithArguments("Source", "Tuple<Base>"));
    }

}
