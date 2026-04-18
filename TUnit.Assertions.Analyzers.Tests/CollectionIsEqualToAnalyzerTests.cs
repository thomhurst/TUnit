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

    [Test]
    public async Task CustomEnumerable_With_EqualsOverride_Not_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Collections;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Assertions;
                using TUnit.Assertions.Extensions;
                using TUnit.Core;

                public class MyBag : IEnumerable<int>
                {
                    public IEnumerator<int> GetEnumerator() => new List<int>().GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    public override bool Equals(object? obj) => obj is MyBag;
                    public override int GetHashCode() => 0;
                }

                public class MyClass
                {
                    [Test]
                    public async Task Test()
                    {
                        await Assert.That(new MyBag()).IsEqualTo(new MyBag());
                    }
                }
                """
            );
    }

    [Test]
    public async Task CustomEnumerable_With_IEquatable_Not_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Assertions;
                using TUnit.Assertions.Extensions;
                using TUnit.Core;

                public class MyBag : IEnumerable<int>, IEquatable<MyBag>
                {
                    public IEnumerator<int> GetEnumerator() => new List<int>().GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    public bool Equals(MyBag? other) => other is not null;
                }

                public class MyClass
                {
                    [Test]
                    public async Task Test()
                    {
                        await Assert.That(new MyBag()).IsEqualTo(new MyBag());
                    }
                }
                """
            );
    }

    [Test]
    public async Task Record_Collection_Not_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Collections;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Assertions;
                using TUnit.Assertions.Extensions;
                using TUnit.Core;

                public record MyRecordBag(int X) : IEnumerable<int>
                {
                    public IEnumerator<int> GetEnumerator() => new List<int> { X }.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }

                public class MyClass
                {
                    [Test]
                    public async Task Test()
                    {
                        await Assert.That(new MyRecordBag(1)).IsEqualTo(new MyRecordBag(1));
                    }
                }
                """
            );
    }
}
