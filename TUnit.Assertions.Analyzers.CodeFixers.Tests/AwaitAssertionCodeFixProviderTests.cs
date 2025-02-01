using NUnit.Framework;
using Verifier = TUnit.Assertions.Analyzers.CodeFixers.Tests.Verifiers.CSharpCodeFixVerifier<TUnit.Assertions.Analyzers.AwaitAssertionAnalyzer, TUnit.Assertions.Analyzers.CodeFixers.AwaitAssertionCodeFixProvider>;
#pragma warning disable CS0162 // Unreachable code detected

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
                        {|#0:Assert.That(1)|}.IsEqualTo(1);
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
                        await Assert.That(1).IsEqualTo(1);
                    }
                }
                """
            );
    }
}