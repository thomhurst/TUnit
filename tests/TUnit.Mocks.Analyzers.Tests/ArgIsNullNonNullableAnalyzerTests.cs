using TUnit.Mocks.Analyzers.Tests.Verifiers;

using Verifier = TUnit.Mocks.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Mocks.Analyzers.ArgIsNullNonNullableAnalyzer>;

namespace TUnit.Mocks.Analyzers.Tests;

public class ArgIsNullNonNullableAnalyzerTests
{
    private const string ArgStub = """
        namespace TUnit.Mocks.Arguments
        {
            public static class Arg
            {
                public static Arg<T> IsNull<T>() => default!;
                public static Arg<T> IsNotNull<T>() => default!;
            }

            public struct Arg<T> { }
        }
        """;

    [Test]
    public async Task IsNull_With_Non_Nullable_Struct_Reports_TM005()
    {
        await Verifier.VerifyAnalyzerAsync(
            ArgStub + """

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mocks.Arguments.Arg.IsNull<int>()|};
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM005_ArgIsNullNonNullableValueType)
                .WithLocation(0)
                .WithArguments("IsNull", "int")
        );
    }

    [Test]
    public async Task IsNotNull_With_Non_Nullable_Struct_Reports_TM005()
    {
        await Verifier.VerifyAnalyzerAsync(
            ArgStub + """

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mocks.Arguments.Arg.IsNotNull<bool>()|};
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM005_ArgIsNullNonNullableValueType)
                .WithLocation(0)
                .WithArguments("IsNotNull", "bool")
        );
    }

    [Test]
    public async Task IsNull_With_Nullable_Value_Type_Does_Not_Report()
    {
        await Verifier.VerifyAnalyzerAsync(
            ArgStub + """

            public class TestClass
            {
                public void Test()
                {
                    TUnit.Mocks.Arguments.Arg.IsNull<int?>();
                }
            }
            """
        );
    }

    [Test]
    public async Task IsNotNull_With_Nullable_Value_Type_Does_Not_Report()
    {
        await Verifier.VerifyAnalyzerAsync(
            ArgStub + """

            public class TestClass
            {
                public void Test()
                {
                    TUnit.Mocks.Arguments.Arg.IsNotNull<int?>();
                }
            }
            """
        );
    }

    [Test]
    public async Task IsNull_With_Reference_Type_Does_Not_Report()
    {
        await Verifier.VerifyAnalyzerAsync(
            ArgStub + """

            public class TestClass
            {
                public void Test()
                {
                    TUnit.Mocks.Arguments.Arg.IsNull<string>();
                }
            }
            """
        );
    }

    [Test]
    public async Task IsNotNull_With_Reference_Type_Does_Not_Report()
    {
        await Verifier.VerifyAnalyzerAsync(
            ArgStub + """

            public class TestClass
            {
                public void Test()
                {
                    TUnit.Mocks.Arguments.Arg.IsNotNull<string>();
                }
            }
            """
        );
    }

    [Test]
    public async Task IsNull_With_Nullable_Reference_Type_Does_Not_Report()
    {
        await Verifier.VerifyAnalyzerAsync(
            ArgStub + """

            #nullable enable
            public class TestClass
            {
                public void Test()
                {
                    TUnit.Mocks.Arguments.Arg.IsNull<string?>();
                }
            }
            """
        );
    }

    [Test]
    public async Task IsNotNull_With_Nullable_Reference_Type_Does_Not_Report()
    {
        await Verifier.VerifyAnalyzerAsync(
            ArgStub + """

            #nullable enable
            public class TestClass
            {
                public void Test()
                {
                    TUnit.Mocks.Arguments.Arg.IsNotNull<string?>();
                }
            }
            """
        );
    }
}
