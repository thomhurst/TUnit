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
}
