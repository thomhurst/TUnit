using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.ForbidRedefiningAttributeUsageAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class ForbidRedefiningAttributeUsageAnalyzerTests
{
    [Test]
    public async Task No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class InheritedNotInParallelAttribute : NotInParallelAttribute
                {
                    public InheritedNotInParallelAttribute() : base("Blah")
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task AttributeUsage_ReturnsError()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using TUnit.Core;
                            
                [{|#0:AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)|}]
                public class InheritedNotInParallelAttribute : NotInParallelAttribute
                {
                    public InheritedNotInParallelAttribute() : base("Blah")
                    {
                    }
                }
                """,

                Verifier.Diagnostic(Rules.DoNotOverrideAttributeUsageMetadata).
                    WithLocation(0)
            );
    }
}