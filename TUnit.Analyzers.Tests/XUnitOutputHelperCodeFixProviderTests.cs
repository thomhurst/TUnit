using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.XUnitOutputHelperAnalyzer>;
using CodeFixer = TUnit.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<TUnit.Analyzers.XUnitOutputHelperAnalyzer, TUnit.Analyzers.CodeFixers.XUnitOutputHelperCodeFixProvider>;

namespace TUnit.Analyzers.Tests;

public class XUnitOutputHelperCodeFixProviderTests
{
    [Test]
    public async Task Xunit_TestOutputHelper_Flagged_And_Fixed()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                using Xunit;
                
                namespace MyNamespace;
                
                public class MyTests
                {
                    {|#0:private readonly ITestOutputHelper _testOutputHelper;|}
                
                    public MyTests({|#1:ITestOutputHelper testOutputHelper|})
                    {
                        {|#2:_testOutputHelper = testOutputHelper|};
                    }
                
                    [Fact]
                    public void Test1()
                    {
                        {|#3:_testOutputHelper.WriteLine("Hello from Test1")|};
                    }
                }
                """,
                [
                Verifier
                    .Diagnostic(Rules.XunitTestOutputHelper)
                    .WithLocation(0),
                Verifier
                    .Diagnostic(Rules.XunitTestOutputHelper)
                    .WithLocation(1),
                Verifier
                    .Diagnostic(Rules.XunitTestOutputHelper)
                    .WithLocation(2),
                Verifier
                    .Diagnostic(Rules.XunitTestOutputHelper)
                    .WithLocation(3)
                ],
                """
                using Xunit;
                
                namespace MyNamespace;
                
                public class MyTests
                {
                    public MyTests()
                    {
                    }
                
                    [Fact]
                    public void Test1()
                    {
                        Console.WriteLine("Hello from Test1");
                    }
                }
                """
            );
    }
}