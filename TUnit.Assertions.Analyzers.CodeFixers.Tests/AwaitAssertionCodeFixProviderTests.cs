using NUnit.Framework;
using Verifier = TUnit.Assertions.Analyzers.CodeFixers.Tests.Verifiers.CSharpCodeFixVerifier<TUnit.Assertions.Analyzers.AwaitAssertionAnalyzer, TUnit.Assertions.Analyzers.CodeFixers.AwaitAssertionCodeFixProvider>;

namespace TUnit.Assertions.Analyzers.CodeFixers.Tests;

public class AwaitAssertionCodeFixProviderTests
{
    [Test]
    public async Task Void_Changes_To_Async_Task()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System.Threading.Tasks;
                using TUnit.Assertions;
                using TUnit.Assertions.Extensions;
                using TUnit.Core;

                public class MyClass
                {
                    public void MyTest()
                    {
                        var one = 1;
                        {|#0:Assert.That(one)|}.IsEqualTo(1);
                    }
                }
                """,
                Verifier.Diagnostic(Rules.AwaitAssertion)
                    .WithLocation(0),
                """
                using System.Threading.Tasks;
                using TUnit.Assertions;
                using TUnit.Assertions.Extensions;
                using TUnit.Core;

                public class MyClass
                {
                    public async Task MyTest()
                    {
                        var one = 1;
                        await Assert.That(one).IsEqualTo(1);
                    }
                }
                """
            );
    }
}