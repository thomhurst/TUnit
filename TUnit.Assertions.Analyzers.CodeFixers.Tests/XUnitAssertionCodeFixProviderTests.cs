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
    
    [Test]
    public async Task Xunit_Collection_Equivalent()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        int[] a = [1];
                        int[] b = [1];
                        {|#0:Xunit.Assert.Equal(a, b)|};
                        {|#1:Xunit.Assert.NotEqual(a, b)|};
                    }
                }
                """,
                [
                    Verifier.Diagnostic(Rules.XUnitAssertion).WithLocation(0),
                    Verifier.Diagnostic(Rules.XUnitAssertion).WithLocation(1)
                ],
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        int[] a = [1];
                        int[] b = [1];
                        Assert.That(b).IsEquivalentTo(a);
                        Assert.That(b).IsNotEquivalentTo(a);
                    }
                }
                """
            );
    }
    
    [Test]
    public async Task Xunit_Within_Tolerance()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        {|#0:Xunit.Assert.Equal(1.0, 1.0, 0.01)|};
                        {|#1:Xunit.Assert.Equal(1.0, 1.0, tolerance: 0.01)|};
                        {|#2:Xunit.Assert.NotEqual(1.0, 1.0, 0.01)|};
                        {|#3:Xunit.Assert.NotEqual(1.0, 1.0, tolerance: 0.01)|};
                    }
                }
                """,
                [
                    Verifier.Diagnostic(Rules.XUnitAssertion).WithLocation(0),
                    Verifier.Diagnostic(Rules.XUnitAssertion).WithLocation(1),
                    Verifier.Diagnostic(Rules.XUnitAssertion).WithLocation(2),
                    Verifier.Diagnostic(Rules.XUnitAssertion).WithLocation(3),
                ],
                """
            using System.Threading.Tasks;

            public class MyClass
            {
                public void MyTest()
                {
                    Assert.That(1.0).IsEqualTo(1.0).Within(0.01);
                    Assert.That(1.0).IsEqualTo(1.0).Within(0.01);
                    Assert.That(1.0).IsNotEqualTo(1.0).Within(0.01);
                    Assert.That(1.0).IsNotEqualTo(1.0).Within(0.01);
                }
            }
            """
            );
    }
}