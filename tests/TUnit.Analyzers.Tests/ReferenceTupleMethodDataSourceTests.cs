using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.TestDataAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class ReferenceTupleMethodDataSourceTests : BaseAnalyzerTests
{
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
    [Arguments("Tuple<string>", "Tuple<object>")]
    [Arguments("Tuple<int>", "Tuple<object>")]
    [Arguments("Tuple<int>", "Tuple<long>")]
    [Arguments("Tuple<int, int>", "Tuple<long, long>")]
    [Arguments("Tuple<Tuple<int>, int>", "Tuple<Tuple<long>, long>")]
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
            using TUnit.Core;
            public class C
            {
                [Test] [{|#0:MethodDataSource(nameof(Row))|}] public void Test({{targetType}} row) {}
                public static {{sourceType}} Row() => default!;
            }
            """,
            Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                .WithLocation(0).WithArguments(sourceElements, targetType));
    }
}
