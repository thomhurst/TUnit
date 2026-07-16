using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.TestDataAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class AttributeClassPropertyRequiredTests
{
    [Test]
    public async Task Attribute_Class_Property_Without_Required_Should_Not_Flag_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;

                public class MyCustomAttribute : System.Attribute
                {
                    [ClassDataSource<string>]
                    public string? TestProperty { get; init; } // Should NOT trigger TUnit0043
                }
                """
            );
    }

    [Test]
    public async Task Attribute_Class_Property_With_Required_Should_Not_Flag_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;

                public class MyCustomAttribute : System.Attribute
                {
                    [ClassDataSource<string>]
                    public required string TestProperty { get; init; } // Should also NOT trigger error
                }
                """
            );
    }

    [Test]
    public async Task Regular_Test_Class_Property_Without_Required_Should_Flag_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyTestClass
                {
                    [ClassDataSource<string>]
                    public string? {|#0:TestProperty|} { get; init; } // SHOULD trigger TUnit0043

                    [Test]
                    public void TestMethod()
                    {
                    }
                }
                """,

                Verifier.Diagnostic(Rules.PropertyRequiredNotSet)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Regular_Test_Class_Property_With_Required_Should_Not_Flag_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyTestClass
                {
                    [ClassDataSource<string>]
                    public required string TestProperty { get; init; } // Should NOT trigger error

                    [Test]
                    public void TestMethod()
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task Indirect_Attribute_Inheritance_Should_Not_Flag_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;

                public class BaseCustomAttribute : System.Attribute
                {
                }

                public class MyCustomAttribute : BaseCustomAttribute
                {
                    [ClassDataSource<string>]
                    public string? TestProperty { get; init; } // Should NOT trigger TUnit0043
                }
                """
            );
    }

    [Test]
    public async Task Multiple_Data_Source_Attributes_On_Attribute_Class_Property()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;

                public class MyCustomAttribute : System.Attribute
                {
                    [Arguments("test")]
                    public string? TestProperty { get; init; } // Should NOT trigger TUnit0043 even with Arguments
                }
                """
            );
    }
}