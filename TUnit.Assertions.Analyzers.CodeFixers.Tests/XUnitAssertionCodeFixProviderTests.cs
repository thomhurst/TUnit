using NUnit.Framework;
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
                """,
                Verifier.Diagnostic(Rules.XUnitAssertion)
                    .WithLocation(0),
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        Assert.That(1).IsEqualTo(1);
                    }
                }
                """
            );
    }
    
    [Test]
    public async Task Xunit_Contains_Predicate_Overload()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        {|#0:Xunit.Assert.Contains(new[] { 22, 75, 19 }, x => x == 22)|};
                    }
                }
                """,
                Verifier.Diagnostic(Rules.XUnitAssertion)
                    .WithLocation(0),
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        Assert.That(new[] { 22, 75, 19 }).Contains(x => x == 22);
                    }
                }
                """
            );
    }
}