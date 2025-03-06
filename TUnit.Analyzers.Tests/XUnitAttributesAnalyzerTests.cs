using NUnit.Framework;
using TUnit.Analyzers.Tests.Extensions;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.XUnitAttributesAnalyzer>;
using CodeFixer = TUnit.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<TUnit.Analyzers.XUnitAttributesAnalyzer, TUnit.Analyzers.CodeFixers.XUnitAttributesCodeFixProvider>;

namespace TUnit.Analyzers.Tests;

public class XUnitAttributesAnalyzerTests
{
    [Test]
    public async Task Test_Attribute_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using Xunit;

                public class MyClass
                {
                    [{|#0:Fact|}]
                    public void MyTest()
                    {
                    }
                }
                """,
                Verifier.Diagnostic(Rules.XunitAttributes).WithLocation(0)
            );
    }
    
    [TestCase("Fact", "Test")]
    [TestCase("Theory", "Test")]
    public async Task Attribute_Can_Be_Fixed(string attribute, string expected)
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                $$"""
                using TUnit.Core;
                using Xunit;

                public class MyClass
                {
                    [{|#0:{{attribute}}|}]
                    public void MyTest()
                    {
                    }
                }
                """.NormalizeLineEndings(),
                Verifier.Diagnostic(Rules.XunitAttributes).WithLocation(0),
                $$"""
                using TUnit.Core;
                using Xunit;

                public class MyClass
                {
                    [{{expected}}]
                    public void MyTest()
                    {
                    }
                }
                """.NormalizeLineEndings()
            );
    }
}