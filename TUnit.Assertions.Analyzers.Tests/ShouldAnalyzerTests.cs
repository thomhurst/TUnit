using AwaitVerifier = TUnit.Assertions.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Assertions.Analyzers.AwaitAssertionAnalyzer>;
using MixVerifier = TUnit.Assertions.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Assertions.Analyzers.MixAndOrOperatorsAnalyzer>;

namespace TUnit.Assertions.Analyzers.Tests;

/// <summary>
/// Verifies the existing analyzers also recognise the Should-flavored entry surface so users
/// of <c>value.Should().X()</c> get the same compile-time guidance as <c>Assert.That(value).X()</c>.
/// </summary>
public class ShouldAnalyzerTests
{
    [Test]
    public async Task Should_chain_not_awaited_is_flagged()
    {
        await AwaitVerifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading.Tasks;
                using TUnit.Assertions.Should;
                using TUnit.Assertions.Should.Extensions;

                public class MyClass
                {
                    public async Task MyTest()
                    {
                        var one = 1;
                        {|#0:one.Should()|}.BeEqualTo(1);
                    }
                }
                """,

                AwaitVerifier.Diagnostic(Rules.AwaitAssertion)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Should_chain_awaited_is_clean()
    {
        await AwaitVerifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading.Tasks;
                using TUnit.Assertions.Should;
                using TUnit.Assertions.Should.Extensions;

                public class MyClass
                {
                    public async Task MyTest()
                    {
                        var one = 1;
                        await one.Should().BeEqualTo(1);
                    }
                }
                """
            );
    }

    [Test]
    public async Task Mixed_And_Or_in_Should_chain_is_flagged()
    {
        await MixVerifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading.Tasks;
                using TUnit.Assertions.Should;
                using TUnit.Assertions.Should.Extensions;

                public class MyClass
                {
                    public async Task MyTest()
                    {
                        var one = 1;
                        {|#0:await one.Should().BeEqualTo(1).And.BeEqualTo(1).Or.BeEqualTo(2)|};
                    }
                }
                """,

                MixVerifier.Diagnostic(Rules.MixAndOrConditionsAssertion)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Pure_And_in_Should_chain_is_clean()
    {
        await MixVerifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading.Tasks;
                using TUnit.Assertions.Should;
                using TUnit.Assertions.Should.Extensions;

                public class MyClass
                {
                    public async Task MyTest()
                    {
                        var one = 1;
                        await one.Should().BeEqualTo(1).And.NotBeEqualTo(7);
                    }
                }
                """
            );
    }

    [Test]
    public async Task Unrelated_Should_extension_in_other_namespace_is_not_flagged()
    {
        // Confirms the analyzer's TUnit-namespace check rules out unrelated `Should()`
        // extensions (e.g. FluentAssertions, custom user libraries) from triggering
        // TUnitAssertions0002.
        await AwaitVerifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading.Tasks;

                namespace OtherLibrary
                {
                    public class Wrapper { public Wrapper Should() => this; public void BeFoo() { } }
                    public static class WrapperExtensions
                    {
                        public static Wrapper Should(this object value) => new Wrapper();
                    }
                }

                public class MyClass
                {
                    public async Task MyTest()
                    {
                        var one = 1;
                        OtherLibrary.WrapperExtensions.Should(one).BeFoo();
                    }
                }
                """
            );
    }
}
