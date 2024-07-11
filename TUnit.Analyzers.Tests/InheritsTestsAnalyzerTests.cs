using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.InheritsTestsAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class InheritsTestsAnalyzerTests
{
    [Test]
    public async Task No_Error()
    {
        const string text = """
                            using TUnit.Core;

                            [InheritsTests]
                            public class Tests : BaseClass
                            {
                            }
                            
                            public class BaseClass
                            {
                                [Test]
                                public void Test()
                                {
                                }
                            }
                            """;

        await Verifier.VerifyAnalyzerAsync(text);
    }
    
    [Test]
    public async Task No_Error2()
    {
        const string text = """
                            using TUnit.Core;

                            public class Tests
                            {
                            }
                            """;

        await Verifier.VerifyAnalyzerAsync(text);
    }
    
    [Test]
    public async Task Warning_Test()
    {
        const string text = """
                            using TUnit.Core;

                            public class {|#0:Tests|} : BaseClass
                            {
                            }

                            public class BaseClass
                            {
                                [Test]
                                public void Test()
                                {
                                }
                            }
                            """;

        var expected = Verifier.Diagnostic(Rules.DoesNotInheritTestsWarning).WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
}