using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.ForbidRedefiningAttributeUsageAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class ForbidRedefiningAttributeUsageAnalyzerTests
{
    [Test]
    public async Task No_Error()
    {
        const string text = """
                            using TUnit.Core;

                            public class InheritedNotInParallelAttribute : NotInParallelAttribute
                            {
                                public InheritedNotInParallelAttribute() : base("Blah")
                                {
                                }
                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
    
    [Test]
    public async Task AttributeUsage_ReturnsError()
    {
        const string text = """
                            using System;
                            using TUnit.Core;
                            
                            [{|#0:AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)|}]
                            public class InheritedNotInParallelAttribute : NotInParallelAttribute
                            {
                                public InheritedNotInParallelAttribute() : base("Blah")
                                {
                                }
                            }
                            """;
        
        var expected = Verifier.Diagnostic(Rules.DoNotOverrideAttributeUsageMetadata).WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
}