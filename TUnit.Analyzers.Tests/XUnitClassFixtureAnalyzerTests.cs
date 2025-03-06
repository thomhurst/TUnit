using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.XUnitClassFixtureAnalyzer>;
using CodeFixer = TUnit.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<TUnit.Analyzers.XUnitClassFixtureAnalyzer, TUnit.Analyzers.CodeFixers.XUnitClassFixtureCodeFixProvider>;

namespace TUnit.Analyzers.Tests;

public class XUnitClassFixtureAnalyzerTests
{
    [Test]
    public async Task ClassFixture_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using Xunit;
                
                public class MyType;

                public class MyClass : {|#0:IClassFixture<MyType>|}
                {
                    [Fact]
                    public void MyTest()
                    {
                    }
                }
                """,
                Verifier.Diagnostic(Rules.XunitClassFixtures).WithLocation(0)
            );
    }
    
    [Test]
    public async Task ClassFixture_Can_Be_Fixed()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                using Xunit;

                public class MyType;

                public class MyClass(MyType myType) : {|#0:IClassFixture<MyType>|}
                {
                    [Fact]
                    public void MyTest()
                    {
                    }
                }
                """.ReplaceLineEndings(),
                Verifier.Diagnostic(Rules.XunitClassFixtures).WithLocation(0),
                """
                using Xunit;
                using TUnit.Core;

                public class MyType;

                [ClassDataSource<MyType>(Shared = SharedType.PerClass)]
                public class MyClass(MyType myType) 
                {
                    [Fact]
                    public void MyTest()
                    {
                    }
                }
                """.ReplaceLineEndings()
            );
    }
}