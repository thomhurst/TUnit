using NUnit.Framework;
using Verifier = TUnit.Assertions.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Assertions.Analyzers.ObjectBaseEqualsMethodAnalyzer>;

namespace TUnit.Assertions.Analyzers.Tests;

public class ObjectBaseEqualsMethodAnalyzerTests
{
    
    [Test]
    public async Task No_Error()
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
                        await Assert.That(1).IsEqualTo(1);
                    }
                }
                """
            );
    }

    [Test]
    public async Task No_Error2()
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
                        await Assert.That(1).IsPositive().And.IsEqualTo(1);
                    }
                }
                """
            );
    }

    [Test]
    public async Task No_Error3()
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
                        await Assert.That(1).IsPositive().Or.IsEqualTo(1);
                    }
                }
                """
            );
    }

    [Test]
    public async Task Assert_With_ObjectEquals_Raises_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading.Tasks;
                using TUnit.Assertions;
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    [System.Obsolete("Obsolete")]
                    public void Test()
                    {
                        {|#0:Assert.That(1).Equals(1)|};
                    }
                }
                """,

                Verifier.Diagnostic(Rules.ObjectEqualsBaseMethod)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task And_With_ObjectEquals_Raises_Error()
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
                    public void Test()
                    {
                        {|#0:Assert.That(1).IsPositive().And.Equals(1)|};
                    }
                }
                """,

                Verifier.Diagnostic(Rules.ObjectEqualsBaseMethod)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Or_With_ObjectEquals_Raises_Error()
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
                    public void Test()
                    {
                        {|#0:Assert.That(1).IsPositive().Or.Equals(1)|};
                    }
                }
                """,

                Verifier.Diagnostic(Rules.ObjectEqualsBaseMethod)
                    .WithLocation(0)
            );
    }
}