using Verifier = TUnit.Assertions.Analyzers.CodeFixers.Tests.Verifiers.CSharpCodeFixVerifier<
    TUnit.Assertions.Analyzers.CollectionIsEqualToAnalyzer,
    TUnit.Assertions.Analyzers.CodeFixers.CollectionIsEqualToCodeFixProvider>;

namespace TUnit.Assertions.Analyzers.CodeFixers.Tests;

public class CollectionIsEqualToCodeFixProviderTests
{
    [Test]
    public async Task Rewrites_IsEqualTo_To_IsEquivalentTo()
    {
        await Verifier
            .VerifyCodeFixAsync(
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
                        var a = new List<int> { 1, 2 };
                        var b = new List<int> { 1, 2 };
                        await Assert.That(a).{|#0:IsEqualTo(b)|};
                    }
                }
                """,
                Verifier.Diagnostic(Rules.CollectionIsEqualToUsesReferenceEquality)
                    .WithLocation(0),
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
                        var a = new List<int> { 1, 2 };
                        var b = new List<int> { 1, 2 };
                        await Assert.That(a).IsEquivalentTo(b);
                    }
                }
                """
            );
    }

    [Test]
    public async Task Fix_Preserves_Chained_Calls()
    {
        await Verifier.VerifyCodeFixAsync(
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
                    var a = new List<int> { 1, 2 };
                    var b = new List<int> { 1, 2 };
                    await Assert.That(a).{|#0:IsEqualTo(b)|}.And.IsNotNull();
                }
            }
            """,
            Verifier.Diagnostic(Rules.CollectionIsEqualToUsesReferenceEquality).WithLocation(0),
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
                    var a = new List<int> { 1, 2 };
                    var b = new List<int> { 1, 2 };
                    await Assert.That(a).IsEquivalentTo(b).And.IsNotNull();
                }
            }
            """);
    }

    [Test]
    public async Task Fix_Works_On_Arrays()
    {
        await Verifier.VerifyCodeFixAsync(
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
                    int[] a = { 1 };
                    int[] b = { 1 };
                    await Assert.That(a).{|#0:IsEqualTo(b)|};
                }
            }
            """,
            Verifier.Diagnostic(Rules.CollectionIsEqualToUsesReferenceEquality).WithLocation(0),
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
                    int[] a = { 1 };
                    int[] b = { 1 };
                    await Assert.That(a).IsEquivalentTo(b);
                }
            }
            """);
    }
}
