using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.XUnitUsingDirectiveAnalyzer>;
using CodeFixer = TUnit.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<TUnit.Analyzers.XUnitUsingDirectiveAnalyzer, TUnit.Analyzers.CodeFixers.XUnitUsingDirectiveCodeFixProvider>;

namespace TUnit.Analyzers.Tests;

public class XUnitUsingDirectiveAnalyzerTests
{
    [Test]
    public async Task Xunit_Directive_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                {|#0:using Xunit;|}
                
                public class MyClass
                {
                    [Test]
                    public void MyTest()
                    {
                    }
                }
                """,
                Verifier.Diagnostic(Rules.XunitUsingDirectives).WithLocation(0)
            );
    }
    
    [Test]
    public async Task Xunit_Directive_Can_Be_Removed()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                using TUnit.Core;
                {|#0:using Xunit;|}

                public class MyClass
                {
                    [Test]
                    public void MyTest()
                    {
                    }
                }
                """,
                Verifier.Diagnostic(Rules.XunitUsingDirectives).WithLocation(0),
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    public void MyTest()
                    {
                    }
                }
                """
            );
    }
}