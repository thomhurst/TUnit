using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.PsvmAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class PsvmAnalyzerTests
{
    [Test]
    [Combinatorial]
    public async Task Main_Method_Raises_Error(
        [Values("", "public", "private", "internal", "protected")] string accessibility, 
        [Values("void", "int", "Task", "Task<int>")] string returnType, 
        [Values("", "string[] args")] string parameters
        )
    {
        var text = $$"""
                            using System.Threading.Tasks;
                            using TUnit.Core;
                            
                            public class Program
                            {
                                {{accessibility}} static {{returnType}} {|#0:Main|}({{parameters}})
                                {
                                    {{GenerateReturnPath(returnType)}};
                                }
                            }

                            public class MyClass
                            {
                                [Test]
                                public async Task Test()
                                {
                                }
                            }
                            """;

        var expected = Verifier
            .Diagnostic(Rules.NoMainMethod)
            .WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected).ConfigureAwait(false);
    }

    private string GenerateReturnPath(string returnType)
    {
        return returnType switch
        {
            "int" => "return 0",
            "Task<int>" => "return Task.FromResult(0)",
            "Task" => "return Task.CompletedTask",
            _ => string.Empty
        };
    }

    [Test]
    public async Task No_Main_Raises_No_Error()
    {
        const string text = """
                            using System.Threading.Tasks;
                            using TUnit.Core;

                            public class MyClass
                            {
                                [Test]
                                public async Task Test()
                                {
                                }
                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text).ConfigureAwait(false);
    }
}