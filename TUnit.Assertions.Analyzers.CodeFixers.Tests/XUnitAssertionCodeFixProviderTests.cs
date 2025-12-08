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

    [Test]
    public async Task Xunit_All_Converts_To_AssertMultiple_WithForeach()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        var users = new[]
                        {
                            new User { Name = "Alice", Age = 25 },
                            new User { Name = "Bob", Age = 30 }
                        };

                        {|#0:Xunit.Assert.All(users, user =>
                        {
                            {|#1:Xunit.Assert.NotNull(user.Name)|};
                            {|#2:Xunit.Assert.True(user.Age > 18)|};
                        })|};
                    }
                }

                public class User
                {
                    public string Name { get; init; }
                    public int Age { get; init; }
                }
                """,
                [
                    Verifier.Diagnostic(Rules.XUnitAssertion).WithLocation(0),
                    Verifier.Diagnostic(Rules.XUnitAssertion).WithLocation(1),
                    Verifier.Diagnostic(Rules.XUnitAssertion).WithLocation(2)
                ],
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    public async Task MyTest()
                    {
                        var users = new[]
                        {
                            new User { Name = "Alice", Age = 25 },
                            new User { Name = "Bob", Age = 30 }
                        };
                        using (Assert.Multiple())
                        {
                            foreach (var user in users)
                            {
                                await Assert.That(user.Name).IsNotNull();
                                await Assert.That(user.Age > 18).IsTrue();
                            }
                        }
                    }
                }

                public class User
                {
                    public string Name { get; init; }
                    public int Age { get; init; }
                }
                """
            );
    }

    [Test]
    public async Task Xunit_True_With_Message_Converts_To_TUnit_With_Because()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        var result = true;
                        {|#0:Xunit.Assert.True(result, "user message if false")|};
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
                        var result = true;
                        Assert.That(result).IsTrue().Because("user message if false");
                    }
                }
                """
            );
    }

    [Test]
    public async Task Xunit_False_With_Message_Converts_To_TUnit_With_Because()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        var result = false;
                        {|#0:Xunit.Assert.False(result, "user message if true")|};
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
                        var result = false;
                        Assert.That(result).IsFalse().Because("user message if true");
                    }
                }
                """
            );
    }

    [Test]
    public async Task Xunit_True_Without_Message_Converts_To_TUnit()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        var result = true;
                        {|#0:Xunit.Assert.True(result)|};
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
                        var result = true;
                        Assert.That(result).IsTrue();
                    }
                }
                """
            );
    }

    [Test]
    public async Task Xunit_False_Without_Message_Converts_To_TUnit()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        var result = false;
                        {|#0:Xunit.Assert.False(result)|};
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
                        var result = false;
                        Assert.That(result).IsFalse();
                    }
                }
                """
            );
    }

    [Test]
    public async Task Xunit_Null_Converts_To_TUnit()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        object? obj = null;
                        {|#0:Xunit.Assert.Null(obj)|};
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
                        object? obj = null;
                        Assert.That(obj).IsNull();
                    }
                }
                """
            );
    }

    [Test]
    public async Task Xunit_NotNull_Converts_To_TUnit()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        object obj = new object();
                        {|#0:Xunit.Assert.NotNull(obj)|};
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
                        object obj = new object();
                        Assert.That(obj).IsNotNull();
                    }
                }
                """
            );
    }

    [Test]
    public async Task Xunit_Same_Converts_To_TUnit()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        var obj = new object();
                        {|#0:Xunit.Assert.Same(obj, obj)|};
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
                        var obj = new object();
                        Assert.That(obj).IsSameReferenceAs(obj);
                    }
                }
                """
            );
    }

    [Test]
    public async Task Xunit_NotSame_Converts_To_TUnit()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        var obj1 = new object();
                        var obj2 = new object();
                        {|#0:Xunit.Assert.NotSame(obj1, obj2)|};
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
                        var obj1 = new object();
                        var obj2 = new object();
                        Assert.That(obj2).IsNotSameReferenceAs(obj1);
                    }
                }
                """
            );
    }

    [Test]
    public async Task Xunit_Contains_String_Converts_To_TUnit()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        {|#0:Xunit.Assert.Contains("needle", "haystack with needle")|};
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
                        Assert.That("haystack with needle").Contains("needle");
                    }
                }
                """
            );
    }

    [Test]
    public async Task Xunit_DoesNotContain_Converts_To_TUnit()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        {|#0:Xunit.Assert.DoesNotContain("missing", "haystack")|};
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
                        Assert.That("haystack").DoesNotContain("missing");
                    }
                }
                """
            );
    }

    [Test]
    public async Task Xunit_StartsWith_Converts_To_TUnit()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        {|#0:Xunit.Assert.StartsWith("hello", "hello world")|};
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
                        Assert.That("hello world").StartsWith("hello");
                    }
                }
                """
            );
    }

    [Test]
    public async Task Xunit_EndsWith_Converts_To_TUnit()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        {|#0:Xunit.Assert.EndsWith("world", "hello world")|};
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
                        Assert.That("hello world").EndsWith("world");
                    }
                }
                """
            );
    }

    [Test]
    public async Task Xunit_Empty_Converts_To_TUnit()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System.Threading.Tasks;
                using System.Collections.Generic;

                public class MyClass
                {
                    public void MyTest()
                    {
                        var list = new List<int>();
                        {|#0:Xunit.Assert.Empty(list)|};
                    }
                }
                """,
                Verifier.Diagnostic(Rules.XUnitAssertion)
                    .WithLocation(0),
                """
                using System.Threading.Tasks;
                using System.Collections.Generic;

                public class MyClass
                {
                    public void MyTest()
                    {
                        var list = new List<int>();
                        Assert.That(list).IsEmpty();
                    }
                }
                """
            );
    }

    [Test]
    public async Task Xunit_NotEmpty_Converts_To_TUnit()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System.Threading.Tasks;
                using System.Collections.Generic;

                public class MyClass
                {
                    public void MyTest()
                    {
                        var list = new List<int> { 1 };
                        {|#0:Xunit.Assert.NotEmpty(list)|};
                    }
                }
                """,
                Verifier.Diagnostic(Rules.XUnitAssertion)
                    .WithLocation(0),
                """
                using System.Threading.Tasks;
                using System.Collections.Generic;

                public class MyClass
                {
                    public void MyTest()
                    {
                        var list = new List<int> { 1 };
                        Assert.That(list).IsNotEmpty();
                    }
                }
                """
            );
    }

    [Test]
    public async Task Xunit_Single_Converts_To_TUnit()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System.Threading.Tasks;
                using System.Collections.Generic;

                public class MyClass
                {
                    public void MyTest()
                    {
                        var list = new List<int> { 1 };
                        {|#0:Xunit.Assert.Single(list)|};
                    }
                }
                """,
                Verifier.Diagnostic(Rules.XUnitAssertion)
                    .WithLocation(0),
                """
                using System.Threading.Tasks;
                using System.Collections.Generic;

                public class MyClass
                {
                    public void MyTest()
                    {
                        var list = new List<int> { 1 };
                        Assert.That(list).HasSingleItem();
                    }
                }
                """
            );
    }

    [Test]
    public async Task Xunit_IsType_Generic_Converts_To_TUnit()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        object obj = "hello";
                        {|#0:Xunit.Assert.IsType<string>(obj)|};
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
                        object obj = "hello";
                        Assert.That(obj).IsTypeOf<string>();
                    }
                }
                """
            );
    }

    [Test]
    public async Task Xunit_IsNotType_Generic_Converts_To_TUnit()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        object obj = "hello";
                        {|#0:Xunit.Assert.IsNotType<int>(obj)|};
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
                        object obj = "hello";
                        Assert.That(obj).IsNotTypeOf<int>();
                    }
                }
                """
            );
    }

    [Test]
    public async Task Xunit_IsAssignableFrom_Generic_Converts_To_TUnit()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System.Threading.Tasks;
                using System.Collections.Generic;

                public class MyClass
                {
                    public void MyTest()
                    {
                        object obj = new List<int>();
                        {|#0:Xunit.Assert.IsAssignableFrom<System.Collections.IEnumerable>(obj)|};
                    }
                }
                """,
                Verifier.Diagnostic(Rules.XUnitAssertion)
                    .WithLocation(0),
                """
                using System.Threading.Tasks;
                using System.Collections.Generic;

                public class MyClass
                {
                    public void MyTest()
                    {
                        object obj = new List<int>();
                        Assert.That(obj).IsAssignableFrom<System.Collections.IEnumerable>();
                    }
                }
                """
            );
    }

    [Test]
    public async Task Xunit_Fail_Converts_To_TUnit()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        {|#0:Xunit.Assert.Fail("This should fail")|};
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
                        Fail.Test();
                    }
                }
                """
            );
    }

    [Test]
    public async Task Xunit_Skip_Converts_To_TUnit()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        {|#0:Xunit.Assert.Skip("Skipping this test")|};
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
                        Skip.Test();
                    }
                }
                """
            );
    }

    [Test]
    public async Task Xunit_Throws_Generic_Converts_To_TUnit()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System;
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        {|#0:Xunit.Assert.Throws<InvalidOperationException>(() => throw new InvalidOperationException())|};
                    }
                }
                """,
                Verifier.Diagnostic(Rules.XUnitAssertion)
                    .WithLocation(0),
                """
                using System;
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        Assert.That(() => throw new InvalidOperationException()).ThrowsExactly<InvalidOperationException>();
                    }
                }
                """
            );
    }

    [Test]
    public async Task Xunit_ThrowsAny_Generic_Converts_To_TUnit()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System;
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        {|#0:Xunit.Assert.ThrowsAny<Exception>(() => throw new InvalidOperationException())|};
                    }
                }
                """,
                Verifier.Diagnostic(Rules.XUnitAssertion)
                    .WithLocation(0),
                """
                using System;
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        Assert.That(() => throw new InvalidOperationException()).Throws<Exception>();
                    }
                }
                """
            );
    }

    [Test]
    public async Task Xunit_ThrowsAsync_Generic_Converts_To_TUnit()
    {
        await Verifier
            .VerifyCodeFixAsync(
                """
                using System;
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        {|#0:Xunit.Assert.ThrowsAsync<InvalidOperationException>(async () => throw new InvalidOperationException())|};
                    }
                }
                """,
                Verifier.Diagnostic(Rules.XUnitAssertion)
                    .WithLocation(0),
                """
                using System;
                using System.Threading.Tasks;

                public class MyClass
                {
                    public void MyTest()
                    {
                        Assert.That(async () => throw new InvalidOperationException()).ThrowsExactly<InvalidOperationException>();
                    }
                }
                """
            );
    }

}
