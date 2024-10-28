using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.SingleTUnitAttributeAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class SingleTUnitAttributeAnalyzerTests
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
                            
                public class MyClass
                {
                    [NotInParallel]
                    public void Blah()
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task InheritedDuplicate_ReturnsError()
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

                public class MyClass
                {
                    [NotInParallel, InheritedNotInParallelAttribute]
                    public void {|#0:Blah|}()
                    {
                    }
                }
                """,

                Verifier.Diagnostic(Rules.DuplicateSingleAttribute)
                    .WithLocation(0)
                    .WithMessage("Duplicate TUnit.Core.NotInParallelAttribute when only 1 is allowed")
            );
    }
}