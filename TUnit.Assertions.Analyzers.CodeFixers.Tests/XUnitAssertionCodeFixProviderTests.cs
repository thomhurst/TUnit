using NUnit.Framework;
using TUnit.Assertions.Analyzers.CodeFixers.Tests.Extensions;
using Verifier = TUnit.Assertions.Analyzers.CodeFixers.Tests.Verifiers.CSharpCodeFixVerifier<TUnit.Assertions.Analyzers.XUnitAssertionAnalyzer, TUnit.Assertions.Analyzers.CodeFixers.XUnitAssertionCodeFixProvider>;
#pragma warning disable CS0162 // Unreachable code detected

namespace TUnit.Assertions.Analyzers.CodeFixers.Tests;

public class XUnitAssertionCodeFixProviderTests
{
    [Test]
    public async Task Xunit_Converts_To_TUnit_Equals()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        {|#0:Xunit.Assert.Equal(1, 1)|};
                    }
                }
                """.NormalizeLineEndings(),
                Verifier.Diagnostic(Rules.XUnitAssertion)
                    .WithLocation(0),
                """
                using System.Threading.Tasks;
                using TUnit.Assertions;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    public void MyTest()
                    {
                        Assert.That(1).IsEqualTo(1);
                    }
                }
                """.NormalizeLineEndings()
            );
    }
}