using Microsoft.CodeAnalysis.Text;
using NUnit.Framework.Legacy;
using CodeFixer = TUnit.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<TUnit.Analyzers.NUnitMigrationAnalyzer, TUnit.Analyzers.CodeFixers.NUnitMigrationCodeFixProvider>;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.NUnitMigrationAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class NUnitMigrationAnalyzerTests
{
    [Test]
    [Arguments("NUnit.Framework.Test")]
    [Arguments("NUnit.Framework.TestCase")]
    public async Task NUnit_Attribute_Flagged(string attribute)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
                using NUnit.Framework;

                public class MyClass
                {
                    {|#0:[{{attribute}}]|}
                    public void MyMethod() { }
                }
                """,
            ConfigureNUnitTest,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0)
        );
    }
    
    [Test]
    [Arguments("NUnit.Framework.Test", "Test")]
    [Arguments("NUnit.Framework.SetUp", "Before(HookType.Test)")]
    [Arguments("NUnit.Framework.TearDown", "After(HookType.Test)")]
    [Arguments("NUnit.Framework.OneTimeSetUp", "Before(HookType.Class)")]
    [Arguments("NUnit.Framework.OneTimeTearDown", "After(HookType.Class)")]
    public async Task NUnit_Attribute_Can_Be_Converted(string attribute, string expected)
    {
        await CodeFixer.VerifyCodeFixAsync(
            $$"""
                using NUnit.Framework;

                public class MyClass
                {
                    {|#0:[{{attribute}}]|}
                    public void MyMethod() { }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            $$"""
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [{{expected}}]
                    public void MyMethod() { }
                }
                """,
            ConfigureNUnitTest
        );
    }
    
    [Test]
    public async Task NUnit_TestFixture_Attribute_Removed()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:[TestFixture]|}
                public class MyClass
                {
                    [Test]
                    public void MyMethod() { }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    public void MyMethod() { }
                }
                """,
            ConfigureNUnitTest
        );
    }
    
    [Test]
    public async Task NUnit_Assert_That_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void MyMethod()
                    {
                        Assert.That(5, Is.EqualTo(5));
                        Assert.That(true, Is.True);
                        Assert.That(null, Is.Null);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    public async Task MyMethod()
                    {
                        await Assert.That(5).IsEqualTo(5);
                        await Assert.That(true).IsTrue();
                        await Assert.That(null).IsNull();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_Classic_Assertions_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;
                using NUnit.Framework.Legacy;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void MyMethod()
                    {
                        ClassicAssert.AreEqual(5, 5);
                        ClassicAssert.IsTrue(true);
                        ClassicAssert.IsNull(null);
                        ClassicAssert.Greater(10, 5);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    public void MyMethod()
                    {
                        ClassicAssert.AreEqual(5, 5);
                        ClassicAssert.IsTrue(true);
                        ClassicAssert.IsNull(null);
                        ClassicAssert.Greater(10, 5);
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }
    
    [Test]
    public async Task NUnit_Directive_Flagged()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
                {|#0:using NUnit.Framework;|}

                public class MyClass
                {
                    public void MyMethod() { }
                }
                """,
            ConfigureNUnitTest,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0)
        );
    }
    
    [Test]
    public async Task NUnit_Directive_Can_Be_Removed()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                {|#0:using NUnit.Framework;|}

                public class MyClass
                {
                    public void MyMethod() { }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    public void MyMethod() { }
                }
                """,
            ConfigureNUnitTest
        );
    }
    
    [Test]
    public async Task NUnit_SetUp_TearDown_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [SetUp]
                    public void Setup() { }

                    [TearDown]
                    public void Teardown() { }

                    [Test]
                    public void MyMethod() { }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Before(HookType.Test)]
                    public void Setup() { }

                    [After(HookType.Test)]
                    public void Teardown() { }

                    [Test]
                    public void MyMethod() { }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_Nested_Class_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class OuterClass|}
                {
                    public class InnerTests
                    {
                        [Test]
                        public void InnerTest()
                        {
                            Assert.That(true, Is.True);
                        }
                    }

                    [Test]
                    public void OuterTest()
                    {
                        Assert.That(false, Is.False);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class OuterClass
                {
                    public class InnerTests
                    {
                        [Test]
                        public async Task InnerTest()
                        {
                            await Assert.That(true).IsTrue();
                        }
                    }

                    [Test]
                    public async Task OuterTest()
                    {
                        await Assert.That(false).IsFalse();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_Generic_Test_Class_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class GenericTestClass|}<T>
                {
                    [Test]
                    public void GenericTest()
                    {
                        var instance = default(T);
                        Assert.That(instance, Is.EqualTo(default(T)));
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class GenericTestClass<T>
                {
                    [Test]
                    public async Task GenericTest()
                    {
                        var instance = default(T);
                        await Assert.That(instance).IsEqualTo(default(T));
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_Complex_Constraint_Chains_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void ComplexConstraints()
                    {
                        Assert.That(10, Is.GreaterThan(5));
                        Assert.That(3, Is.LessThan(10));
                        Assert.That("hello", Is.Not.Null);
                        Assert.That("test", Contains.Substring("es"));
                        Assert.That("world", Does.StartWith("wor"));
                        Assert.That("hello", Does.EndWith("llo"));
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    public async Task ComplexConstraints()
                    {
                        await Assert.That(10).IsGreaterThan(5);
                        await Assert.That(3).IsLessThan(10);
                        await Assert.That("hello").IsNotNull();
                        await Assert.That("test").Contains("es");
                        await Assert.That("world").StartsWith("wor");
                        await Assert.That("hello").EndsWith("llo");
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_Complete_File_Transformation()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;
                using System;

                {|#0:[TestFixture]|}
                public class CompleteTestClass
                {
                    private int _counter;

                    [OneTimeSetUp]
                    public void ClassSetup()
                    {
                        _counter = 0;
                    }

                    [SetUp]
                    public void Setup()
                    {
                        _counter++;
                    }

                    [Test]
                    public void Test1()
                    {
                        Assert.That(_counter, Is.GreaterThan(0));
                        ClassicAssert.IsTrue(true);
                    }

                    [TestCase(1, 2, 3)]
                    [TestCase(5, 5, 10)]
                    public void AdditionTest(int a, int b, int expected)
                    {
                        var result = a + b;
                        Assert.That(result, Is.EqualTo(expected));
                    }

                    [TestCaseSource(nameof(GetTestData))]
                    public void DataDrivenTest(string input)
                    {
                        Assert.That(input, Is.Not.Null);
                    }

                    public static object[] GetTestData()
                    {
                        return new object[] { "test1", "test2" };
                    }

                    [TearDown]
                    public void Teardown()
                    {
                        // Cleanup
                    }

                    [OneTimeTearDown]
                    public void ClassTeardown()
                    {
                        _counter = 0;
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System;
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class CompleteTestClass
                {
                    private int _counter;

                    [Before(HookType.Class)]
                    public void ClassSetup()
                    {
                        _counter = 0;
                    }

                    [Before(HookType.Test)]
                    public void Setup()
                    {
                        _counter++;
                    }

                    [Test]
                    public async Task Test1()
                    {
                        await Assert.That(_counter).IsGreaterThan(0);
                        ClassicAssert.IsTrue(true);
                    }

                    [Test]
                    [Arguments(1, 2, 3)]
                    [Arguments(5, 5, 10)]
                    public async Task AdditionTest(int a, int b, int expected)
                    {
                        var result = a + b;
                        await Assert.That(result).IsEqualTo(expected);
                    }

                    [Test]
                    [MethodDataSource(nameof(GetTestData))]
                    public async Task DataDrivenTest(string input)
                    {
                        await Assert.That(input).IsNotNull();
                    }

                    public static object[] GetTestData()
                    {
                        return new object[] { "test1", "test2" };
                    }

                    [After(HookType.Test)]
                    public void Teardown()
                    {
                        // Cleanup
                    }

                    [After(HookType.Class)]
                    public void ClassTeardown()
                    {
                        _counter = 0;
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_Multiple_Assertions_In_Single_Test()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;
                using NUnit.Framework.Legacy;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMultipleAssertions()
                    {
                        var value = 42;
                        Assert.That(value, Is.Not.Null);
                        ClassicAssert.IsNotNull(value);
                        ClassicAssert.AreEqual(42, value);
                        Assert.That(value, Is.GreaterThan(0));
                        ClassicAssert.Less(0, value);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    public async Task TestMultipleAssertions()
                    {
                        var value = 42;
                        await Assert.That(value).IsNotNull();
                        ClassicAssert.IsNotNull(value);
                        ClassicAssert.AreEqual(42, value);
                        await Assert.That(value).IsGreaterThan(0);
                        ClassicAssert.Less(0, value);
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_ExpectedResult_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
            using NUnit.Framework;

            public class MyClass
            {
                {|#0:[TestCase(2, 3, ExpectedResult = 5)]|}
                public int Add(int a, int b) => a + b;
            }
            """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
            using System.Threading.Tasks;
            using TUnit.Core;
            using TUnit.Assertions;
            using static TUnit.Assertions.Assert;
            using TUnit.Assertions.Extensions;

            public class MyClass
            {
                [Test]
                [Arguments(2, 3, 5)]
                public async Task Add(int a, int b, int expected)
                {
                    await Assert.That(a + b).IsEqualTo(expected);
                }
            }
            """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_Multiple_ExpectedResult_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
            using NUnit.Framework;

            public class MyClass
            {
                {|#0:[TestCase(2, 3, ExpectedResult = 5)]|}
                [TestCase(10, 5, ExpectedResult = 15)]
                [TestCase(0, 0, ExpectedResult = 0)]
                public int Add(int a, int b) => a + b;
            }
            """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
            using System.Threading.Tasks;
            using TUnit.Core;
            using TUnit.Assertions;
            using static TUnit.Assertions.Assert;
            using TUnit.Assertions.Extensions;

            public class MyClass
            {
                [Test]
                [Arguments(2, 3, 5)]
                [Arguments(10, 5, 15)]
                [Arguments(0, 0, 0)]
                public async Task Add(int a, int b, int expected)
                {
                    await Assert.That(a + b).IsEqualTo(expected);
                }
            }
            """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_ExpectedResult_BlockBody_SingleReturn_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
            using NUnit.Framework;

            public class MyClass
            {
                {|#0:[TestCase(2, 3, ExpectedResult = 5)]|}
                public int Add(int a, int b)
                {
                    var sum = a + b;
                    return sum;
                }
            }
            """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
            using System.Threading.Tasks;
            using TUnit.Core;
            using TUnit.Assertions;
            using static TUnit.Assertions.Assert;
            using TUnit.Assertions.Extensions;

            public class MyClass
            {
                [Test]
                [Arguments(2, 3, 5)]
                public async Task Add(int a, int b, int expected)
                {
                    var sum = a + b;
                    await Assert.That(sum).IsEqualTo(expected);
                }
            }
            """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_ExpectedResult_String_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
            using NUnit.Framework;

            public class MyClass
            {
                {|#0:[TestCase("hello", ExpectedResult = "HELLO")]|}
                [TestCase("World", ExpectedResult = "WORLD")]
                public string ToUpper(string input) => input.ToUpper();
            }
            """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
            using System.Threading.Tasks;
            using TUnit.Core;
            using TUnit.Assertions;
            using static TUnit.Assertions.Assert;
            using TUnit.Assertions.Extensions;

            public class MyClass
            {
                [Test]
                [Arguments("hello", "HELLO")]
                [Arguments("World", "WORLD")]
                public async Task ToUpper(string input, string expected)
                {
                    await Assert.That(input.ToUpper()).IsEqualTo(expected);
                }
            }
            """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_IsNotEqualTo_Converted_Correctly()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        Assert.That(5, Is.Not.EqualTo(10));
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    public async Task TestMethod()
                    {
                        await Assert.That(5).IsNotEqualTo(10);
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_IsNotGreaterThan_Converted_To_IsLessThanOrEqualTo()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        Assert.That(5, Is.Not.GreaterThan(10));
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    public async Task TestMethod()
                    {
                        await Assert.That(5).IsLessThanOrEqualTo(10);
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_DoesNotContain_Converted_Correctly()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        Assert.That("hello world", Does.Not.Contain("foo"));
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    public async Task TestMethod()
                    {
                        await Assert.That("hello world").DoesNotContain("foo");
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_DoesNotStartWith_Converted_Correctly()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        Assert.That("hello world", Does.Not.StartWith("foo"));
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    public async Task TestMethod()
                    {
                        await Assert.That("hello world").DoesNotStartWith("foo");
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_AssertionMessage_Converted_To_Because()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        Assert.That(5, Is.EqualTo(5), "Values should match");
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    public async Task TestMethod()
                    {
                        await Assert.That(5).IsEqualTo(5).Because("Values should match");
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_AssertionMessage_WithNegation_Converted_To_Because()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        Assert.That(5, Is.Not.EqualTo(10), "Hash should differ");
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    public async Task TestMethod()
                    {
                        await Assert.That(5).IsNotEqualTo(10).Because("Hash should differ");
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_VoidMethod_ConvertedToAsyncTask_WhenContainsAwait()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        Assert.That(true, Is.True);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    public async Task TestMethod()
                    {
                        await Assert.That(true).IsTrue();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_TestCaseOnly_AddsTestAttribute()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [TestCase(1, 2, 3)]
                    [TestCase(4, 5, 9)]
                    public void AdditionTest(int a, int b, int expected)
                    {
                        Assert.That(a + b, Is.EqualTo(expected));
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    [Arguments(1, 2, 3)]
                    [Arguments(4, 5, 9)]
                    public async Task AdditionTest(int a, int b, int expected)
                    {
                        await Assert.That(a + b).IsEqualTo(expected);
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_TestCaseWithExistingTest_DoesNotDuplicateTestAttribute()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    [TestCase(1, 2, 3)]
                    public void AdditionTest(int a, int b, int expected)
                    {
                        Assert.That(a + b, Is.EqualTo(expected));
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    [Arguments(1, 2, 3)]
                    public async Task AdditionTest(int a, int b, int expected)
                    {
                        await Assert.That(a + b).IsEqualTo(expected);
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_TestCase_TestName_Converted_To_DisplayName()
    {
        // TestName is now converted to inline DisplayName on each [Arguments]
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [TestCase(1, TestName = "Test values")]
                    [TestCase(2, TestName = "Test values")]
                    public void MyTest(int value)
                    {
                        Assert.That(value > 0, Is.True);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    [Arguments(1, DisplayName = "Test values")]
                    [Arguments(2, DisplayName = "Test values")]
                    public async Task MyTest(int value)
                    {
                        await Assert.That(value > 0).IsTrue();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_TestCase_Different_TestNames_Converted_Inline()
    {
        // Each TestCase's TestName is now converted to inline DisplayName on each [Arguments]
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [TestCase(1, TestName = "Addition of one")]
                    [TestCase(2, TestName = "Addition of two")]
                    public void MyTest(int value)
                    {
                        Assert.That(value > 0, Is.True);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    [Arguments(1, DisplayName = "Addition of one")]
                    [Arguments(2, DisplayName = "Addition of two")]
                    public async Task MyTest(int value)
                    {
                        await Assert.That(value > 0).IsTrue();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_TestCase_Category_Converted_To_Categories_Inline()
    {
        // Category is now converted to inline Categories array on each [Arguments]
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [TestCase(1, Category = "Unit")]
                    [TestCase(2, Category = "Integration")]
                    public void MyTest(int value)
                    {
                        Assert.That(value > 0, Is.True);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    [Arguments(1, Categories = ["Unit"])]
                    [Arguments(2, Categories = ["Integration"])]
                    public async Task MyTest(int value)
                    {
                        await Assert.That(value > 0).IsTrue();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_TestCase_Description_Converted_To_Property()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [TestCase(1, Description = "Tests positive numbers")]
                    public void MyTest(int value)
                    {
                        Assert.That(value > 0, Is.True);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    [Arguments(1)]
                    [Property("Description", "Tests positive numbers")]
                    public async Task MyTest(int value)
                    {
                        await Assert.That(value > 0).IsTrue();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_TestCase_Author_Converted_To_Property()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [TestCase(1, Author = "John Doe")]
                    public void MyTest(int value)
                    {
                        Assert.That(value > 0, Is.True);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    [Arguments(1)]
                    [Property("Author", "John Doe")]
                    public async Task MyTest(int value)
                    {
                        await Assert.That(value > 0).IsTrue();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_TestCase_Explicit_Converted_To_Explicit()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [TestCase(1, Explicit = true)]
                    public void MyTest(int value)
                    {
                        Assert.That(value > 0, Is.True);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    [Arguments(1)]
                    [Explicit]
                    public async Task MyTest(int value)
                    {
                        await Assert.That(value > 0).IsTrue();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_TestCase_ExplicitReason_Converted_To_Explicit_And_Property()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [TestCase(1, ExplicitReason = "Manual test only")]
                    public void MyTest(int value)
                    {
                        Assert.That(value > 0, Is.True);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    [Arguments(1)]
                    [Explicit]
                    [Property("ExplicitReason", "Manual test only")]
                    public async Task MyTest(int value)
                    {
                        await Assert.That(value > 0).IsTrue();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_TestCase_MultipleProperties_All_Converted()
    {
        // TestName and Category are inline on [Arguments], Description/Author become separate [Property] attributes
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [TestCase(1, TestName = "Test One", Category = "Unit", Description = "First test", Author = "Jane")]
                    public void MyTest(int value)
                    {
                        Assert.That(value > 0, Is.True);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    [Arguments(1, DisplayName = "Test One", Categories = ["Unit"])]
                    [Property("Description", "First test")]
                    [Property("Author", "Jane")]
                    public async Task MyTest(int value)
                    {
                        await Assert.That(value > 0).IsTrue();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_TestCase_WithExpectedResult_And_Properties_Converted()
    {
        // TestName and Category are inline on [Arguments], ExpectedResult becomes extra parameter
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                public class MyClass
                {
                    {|#0:[TestCase(2, 3, ExpectedResult = 5, TestName = "Add small numbers", Category = "Math")]|}
                    public int Add(int a, int b) => a + b;
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    [Arguments(2, 3, 5, DisplayName = "Add small numbers", Categories = ["Math"])]
                    public async Task Add(int a, int b, int expected)
                    {
                        await Assert.That(a + b).IsEqualTo(expected);
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_TestCase_Ignore_Converted_To_Skip_Inline()
    {
        // Ignore is converted to inline Skip on each [Arguments]
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [TestCase(1, Ignore = "Needs fixing")]
                    [TestCase(2, Ignore = "Also broken")]
                    public void MyTest(int value)
                    {
                        Assert.That(value > 0, Is.True);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    [Arguments(1, Skip = "Needs fixing")]
                    [Arguments(2, Skip = "Also broken")]
                    public async Task MyTest(int value)
                    {
                        await Assert.That(value > 0).IsTrue();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_TestCase_IgnoreReason_Converted_To_Skip_Inline()
    {
        // IgnoreReason is converted to inline Skip on each [Arguments]
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [TestCase(1, IgnoreReason = "Under development")]
                    public void MyTest(int value)
                    {
                        Assert.That(value > 0, Is.True);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    [Arguments(1, Skip = "Under development")]
                    public async Task MyTest(int value)
                    {
                        await Assert.That(value > 0).IsTrue();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_TestCase_AllInlineProperties_Converted()
    {
        // Test with all inline properties: DisplayName, Skip, and Categories
        // Note: The order of named properties in output follows the order in the source
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [TestCase(1, TestName = "Test One", Category = "Unit", Ignore = "Temporarily disabled")]
                    [TestCase(2, TestName = "Test Two", Category = "Integration", Ignore = "WIP")]
                    public void MyTest(int value)
                    {
                        Assert.That(value > 0, Is.True);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    [Arguments(1, DisplayName = "Test One", Skip = "Temporarily disabled", Categories = ["Unit"])]
                    [Arguments(2, DisplayName = "Test Two", Skip = "WIP", Categories = ["Integration"])]
                    public async Task MyTest(int value)
                    {
                        await Assert.That(value > 0).IsTrue();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_TestCase_AllProperties_Comprehensive_Converted()
    {
        // Comprehensive test with ALL properties - inline (TestName→DisplayName, Category→Categories, Ignore→Skip)
        // and separate attributes (Description→Property, Author→Property, Explicit→Explicit, ExplicitReason→Explicit+Property)
        // Note: The order of named properties in output follows the order in the source
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [TestCase(1, TestName = "Full featured test", Category = "Comprehensive", Ignore = "Testing migration", Description = "A complete test case", Author = "Developer")]
                    public void MyTest(int value)
                    {
                        Assert.That(value > 0, Is.True);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    [Arguments(1, DisplayName = "Full featured test", Skip = "Testing migration", Categories = ["Comprehensive"])]
                    [Property("Description", "A complete test case")]
                    [Property("Author", "Developer")]
                    public async Task MyTest(int value)
                    {
                        await Assert.That(value > 0).IsTrue();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    private static void ConfigureNUnitTest(Verifier.Test test)
    {
        test.TestState.AdditionalReferences.Add(typeof(NUnit.Framework.TestAttribute).Assembly);
    }

    private static void ConfigureNUnitTest(CodeFixer.Test test)
    {
        test.TestState.AdditionalReferences.Add(typeof(NUnit.Framework.TestAttribute).Assembly);
        test.TestState.AdditionalReferences.Add(typeof(NUnit.Framework.Legacy.ClassicAssert).Assembly);
        // FixedState should only have TUnit assemblies, not NUnit
        test.FixedState.AdditionalReferences.Add(typeof(TUnit.Core.TestAttribute).Assembly);
        test.FixedState.AdditionalReferences.Add(typeof(TUnit.Assertions.Assert).Assembly);
    }
}
