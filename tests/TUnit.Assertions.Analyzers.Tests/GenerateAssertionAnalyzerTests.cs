using Verifier = TUnit.Assertions.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Assertions.Analyzers.GenerateAssertionAnalyzer>;

namespace TUnit.Assertions.Analyzers.Tests;

public class GenerateAssertionAnalyzerTests
{
    [Test]
    public async Task Valid_Bool_Extension_Method_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Assertions.Attributes;

                public static class MyAssertions
                {
                    [GenerateAssertion]
                    public static bool IsPositive(this int value)
                    {
                        return value > 0;
                    }
                }
                """
            );
    }

    [Test]
    public async Task Valid_AssertionResult_Extension_Method_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Assertions.Attributes;
                using TUnit.Assertions.Core;

                public static class MyAssertions
                {
                    [GenerateAssertion]
                    public static AssertionResult IsEven(this int value)
                    {
                        return value % 2 == 0 ? AssertionResult.Passed : AssertionResult.Failed("odd");
                    }
                }
                """
            );
    }

    [Test]
    public async Task Valid_Task_Bool_Extension_Method_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading.Tasks;
                using TUnit.Assertions.Attributes;

                public static class MyAssertions
                {
                    [GenerateAssertion]
                    public static async Task<bool> IsPositiveAsync(this int value)
                    {
                        await Task.Delay(1);
                        return value > 0;
                    }
                }
                """
            );
    }

    [Test]
    public async Task Valid_Task_AssertionResult_Extension_Method_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading.Tasks;
                using TUnit.Assertions.Attributes;
                using TUnit.Assertions.Core;

                public static class MyAssertions
                {
                    [GenerateAssertion]
                    public static async Task<AssertionResult> IsEvenAsync(this int value)
                    {
                        await Task.Delay(1);
                        return value % 2 == 0 ? AssertionResult.Passed : AssertionResult.Failed("odd");
                    }
                }
                """
            );
    }

    [Test]
    public async Task Non_Static_Method_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Assertions.Attributes;

                public class MyAssertions
                {
                    [GenerateAssertion]
                    public bool {|#0:IsPositive|}(int value)
                    {
                        return value > 0;
                    }
                }
                """,

                Verifier.Diagnostic(Rules.GenerateAssertionMethodMustBeStatic)
                    .WithLocation(0)
                    .WithArguments("IsPositive"),
                Verifier.Diagnostic(Rules.GenerateAssertionShouldBeExtensionMethod)
                    .WithLocation(0)
                    .WithArguments("IsPositive")
            );
    }

    [Test]
    public async Task Method_Without_Parameters_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Assertions.Attributes;

                public static class MyAssertions
                {
                    [GenerateAssertion]
                    public static bool {|#0:AlwaysTrue|}()
                    {
                        return true;
                    }
                }
                """,

                Verifier.Diagnostic(Rules.GenerateAssertionMethodMustHaveParameter)
                    .WithLocation(0)
                    .WithArguments("AlwaysTrue")
            );
    }

    [Test]
    public async Task Invalid_Return_Type_Void_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Assertions.Attributes;

                public static class MyAssertions
                {
                    [GenerateAssertion]
                    public static void {|#0:DoNothing|}(int value)
                    {
                    }
                }
                """,

                Verifier.Diagnostic(Rules.GenerateAssertionInvalidReturnType)
                    .WithLocation(0)
                    .WithArguments("DoNothing"),
                Verifier.Diagnostic(Rules.GenerateAssertionShouldBeExtensionMethod)
                    .WithLocation(0)
                    .WithArguments("DoNothing")
            );
    }

    [Test]
    public async Task Invalid_Return_Type_String_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Assertions.Attributes;

                public static class MyAssertions
                {
                    [GenerateAssertion]
                    public static string {|#0:GetValue|}(int value)
                    {
                        return value.ToString();
                    }
                }
                """,

                Verifier.Diagnostic(Rules.GenerateAssertionInvalidReturnType)
                    .WithLocation(0)
                    .WithArguments("GetValue"),
                Verifier.Diagnostic(Rules.GenerateAssertionShouldBeExtensionMethod)
                    .WithLocation(0)
                    .WithArguments("GetValue")
            );
    }

    [Test]
    public async Task Non_Extension_Method_Shows_Warning()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Assertions.Attributes;

                public static class MyAssertions
                {
                    [GenerateAssertion]
                    public static bool {|#0:IsPositive|}(int value)
                    {
                        return value > 0;
                    }
                }
                """,

                Verifier.Diagnostic(Rules.GenerateAssertionShouldBeExtensionMethod)
                    .WithLocation(0)
                    .WithArguments("IsPositive")
            );
    }

    [Test]
    public async Task Multiple_Errors_All_Reported()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Assertions.Attributes;

                public class MyAssertions
                {
                    [GenerateAssertion]
                    public void {|#0:Invalid|}()
                    {
                    }
                }
                """,

                // Non-static
                Verifier.Diagnostic(Rules.GenerateAssertionMethodMustBeStatic)
                    .WithLocation(0)
                    .WithArguments("Invalid"),
                // No parameters
                Verifier.Diagnostic(Rules.GenerateAssertionMethodMustHaveParameter)
                    .WithLocation(0)
                    .WithArguments("Invalid"),
                // Invalid return type
                Verifier.Diagnostic(Rules.GenerateAssertionInvalidReturnType)
                    .WithLocation(0)
                    .WithArguments("Invalid")
            );
    }

    [Test]
    public async Task Method_With_Multiple_Parameters_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Assertions.Attributes;

                public static class MyAssertions
                {
                    [GenerateAssertion]
                    public static bool IsBetween(this int value, int min, int max)
                    {
                        return value >= min && value <= max;
                    }
                }
                """
            );
    }
}
