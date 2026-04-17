using Verifier = TUnit.Assertions.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Assertions.Analyzers.CollectionIsEqualToAnalyzer>;

namespace TUnit.Assertions.Analyzers.Tests;

public class CollectionIsEqualToAnalyzerTests
{
    [Test]
    public async Task List_IsEqualTo_Raises_Info()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Assertions;
                using TUnit.Assertions.Extensions;
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    public async Task Test()
                    {
                        var a = new List<int> { 1, 2, 3 };
                        var b = new List<int> { 1, 2, 3 };
                        await Assert.That(a).{|#0:IsEqualTo(b)|};
                    }
                }
                """,
                Verifier.Diagnostic(Rules.CollectionIsEqualToUsesReferenceEquality)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task String_Not_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading.Tasks;
                using TUnit.Assertions;
                using TUnit.Assertions.Extensions;
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    public async Task Test()
                    {
                        await Assert.That("abc").IsEqualTo("abc");
                    }
                }
                """
            );
    }

    [Test]
    public async Task Int_Not_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading.Tasks;
                using TUnit.Assertions;
                using TUnit.Assertions.Extensions;
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    public async Task Test() => await Assert.That(1).IsEqualTo(1);
                }
                """
            );
    }

    [Test]
    public async Task Array_IsEqualTo_Raises_Info()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading.Tasks;
                using TUnit.Assertions;
                using TUnit.Assertions.Extensions;
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    public async Task Test()
                    {
                        int[] a = { 1, 2 };
                        int[] b = { 1, 2 };
                        await Assert.That(a).{|#0:IsEqualTo(b)|};
                    }
                }
                """,
                Verifier.Diagnostic(Rules.CollectionIsEqualToUsesReferenceEquality)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Count_IsEqualTo_Not_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Assertions;
                using TUnit.Assertions.Extensions;
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    public async Task Test()
                    {
                        var list = new List<int> { 1, 2 };
                        await Assert.That(list).Count().IsEqualTo(2);
                    }
                }
                """
            );
    }
}
