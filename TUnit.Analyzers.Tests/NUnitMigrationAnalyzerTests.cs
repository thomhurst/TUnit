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

    [Test]
    public async Task NUnit_ThrowsAsync_Generic_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;
                using System;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        Assert.ThrowsAsync<ArgumentException>(async () => await SomeAsyncMethod());
                    }
                    
                    private async System.Threading.Tasks.Task SomeAsyncMethod() => throw new ArgumentException();
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

                public class MyClass
                {
                    [Test]
                    public async Task TestMethod()
                    {
                        await Assert.ThrowsAsync<ArgumentException>(async () => await SomeAsyncMethod());
                    }
                    
                    private async System.Threading.Tasks.Task SomeAsyncMethod() => throw new ArgumentException();
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_ThrowsAsync_WithConstraint_TypeOf_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;
                using System;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        Assert.ThrowsAsync(Is.TypeOf(typeof(ArgumentException)), async () => await SomeAsyncMethod());
                    }
                    
                    private async System.Threading.Tasks.Task SomeAsyncMethod() => throw new ArgumentException();
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

                public class MyClass
                {
                    [Test]
                    public async Task TestMethod()
                    {
                        await Assert.ThrowsAsync<ArgumentException>(async () => await SomeAsyncMethod());
                    }
                    
                    private async System.Threading.Tasks.Task SomeAsyncMethod() => throw new ArgumentException();
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_ThrowsAsync_WithConstraint_TypeOf_With_Code_To_Execute()
    {
        // This is the exact scenario from the bug report
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;
                using System;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        var sut = new Sut();
                        Assert.ThrowsAsync(Is.TypeOf(typeof(ArgumentException)), async () => await sut.Execute(10));
                    }
                }
                
                public class Sut
                {
                    public async System.Threading.Tasks.Task Execute(int value)
                    {
                        await System.Threading.Tasks.Task.Delay(1);
                        throw new ArgumentException();
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

                public class MyClass
                {
                    [Test]
                    public async Task TestMethod()
                    {
                        var sut = new Sut();
                        await Assert.ThrowsAsync<ArgumentException>(async () => await sut.Execute(10));
                    }
                }
                
                public class Sut
                {
                    public async System.Threading.Tasks.Task Execute(int value)
                    {
                        await System.Threading.Tasks.Task.Delay(1);
                        throw new ArgumentException();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_Throws_Generic_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;
                using System;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        Assert.Throws<ArgumentException>(() => throw new ArgumentException());
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

                public class MyClass
                {
                    [Test]
                    public async Task TestMethod()
                    {
                        await Assert.ThrowsAsync<ArgumentException>(() => throw new ArgumentException());
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_Throws_WithConstraint_TypeOf_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;
                using System;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        Assert.Throws(Is.TypeOf(typeof(ArgumentException)), () => throw new ArgumentException());
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

                public class MyClass
                {
                    [Test]
                    public async Task TestMethod()
                    {
                        await Assert.ThrowsAsync<ArgumentException>(() => throw new ArgumentException());
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_ThrowsAsync_WithUnrecognizedConstraint_PreservesAction()
    {
        // Test that unrecognized constraint patterns still preserve the action lambda
        // This tests the fallback path in ConvertNUnitThrows
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;
                using System;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        // Using Is.InstanceOf which is not recognized by TryExtractTypeFromConstraint
                        Assert.ThrowsAsync(Is.InstanceOf<ArgumentException>(), async () => await SomeMethod());
                    }
                    
                    private async System.Threading.Tasks.Task SomeMethod()
                    {
                        await System.Threading.Tasks.Task.Delay(1);
                        throw new ArgumentException();
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

                public class MyClass
                {
                    [Test]
                    public async Task TestMethod()
                    {
                        // Using Is.InstanceOf which is not recognized by TryExtractTypeFromConstraint
                        await Assert.That(async () => await SomeMethod()).Throws();
                    }
                    
                    private async System.Threading.Tasks.Task SomeMethod()
                    {
                        await System.Threading.Tasks.Task.Delay(1);
                        throw new ArgumentException();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_IsTypeOf_Generic_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        object value = "test";
                        Assert.That(value, Is.TypeOf<string>());
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
                        object value = "test";
                        await Assert.That(value).IsTypeOf<string>();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_IsInstanceOf_Generic_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        object value = "test";
                        Assert.That(value, Is.InstanceOf<string>());
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
                        object value = "test";
                        await Assert.That(value).IsAssignableTo<string>();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_IsNotTypeOf_Generic_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        object value = "test";
                        Assert.That(value, Is.Not.TypeOf<int>());
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
                        object value = "test";
                        await Assert.That(value).IsNotTypeOf<int>();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_IsNotInstanceOf_Generic_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        object value = "test";
                        Assert.That(value, Is.Not.InstanceOf<int>());
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
                        object value = "test";
                        await Assert.That(value).IsNotAssignableTo<int>();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_HasMember_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;
                using System.Collections.Generic;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        var list = new List<int> { 1, 2, 3 };
                        Assert.That(list, Has.Member(2));
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Collections.Generic;
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
                        var list = new List<int> { 1, 2, 3 };
                        await Assert.That(list).Contains(2);
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_Apartment_STA_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;
                using System.Threading;

                {|#0:public class MyClass|}
                {
                    [Test]
                    [Apartment(ApartmentState.STA)]
                    public void TestMethod()
                    {
                        Assert.That(true, Is.True);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Threading;
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    [STAThreadExecutor]
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
    public async Task NUnit_IsTypeOf_Generic_WithMessage_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        object value = "test";
                        Assert.That(value, Is.TypeOf<string>(), "Type should be string");
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
                        object value = "test";
                        await Assert.That(value).IsTypeOf<string>().Because("Type should be string");
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_WithinDelta_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        Assert.That(10, Is.EqualTo(5).Within(2));
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
                        await Assert.That(10).IsEqualTo(5).Within(2);
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_DoesNotThrow_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        int x = 1;
                        int y = 2;
                        Assert.DoesNotThrow(() => x += y);
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
                        int x = 1;
                        int y = 2;
                        await Assert.That(() => x += y).ThrowsNothing();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_Ignore_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        Assert.Ignore("Feature not supported");
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
                    public void TestMethod()
                    {
                        Skip.Test("Feature not supported");
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_Fail_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        Assert.Fail("Test failed intentionally");
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
                    public void TestMethod()
                    {
                        Fail.Test("Test failed intentionally");
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_AssertMultiple_Lambda_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        int x = 1;
                        int y = 2;
                        Assert.Multiple(() =>
                        {
                            Assert.That(x, Is.EqualTo(1));
                            Assert.That(y, Is.EqualTo(2));
                        });
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
                        int x = 1;
                        int y = 2;
                        using (Assert.Multiple())
                        {
                            await Assert.That(x).IsEqualTo(1);
                            await Assert.That(y).IsEqualTo(2);
                        }
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_HasCount_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;
                using System.Collections.Generic;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        var list = new List<int> { 1, 2, 3, 4, 5 };
                        Assert.That(list, Has.Count.EqualTo(5));
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Collections.Generic;
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
                        var list = new List<int> { 1, 2, 3, 4, 5 };
                        await Assert.That(list).Count().IsEqualTo(5);
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_HasExactlyItems_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;
                using System.Collections.Generic;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        var list = new List<int> { 1, 2, 3, 4, 5 };
                        Assert.That(list, Has.Exactly(5).Items);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Collections.Generic;
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
                        var list = new List<int> { 1, 2, 3, 4, 5 };
                        await Assert.That(list).Count().IsEqualTo(5);
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_IsNotZero_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        int value = 42;
                        Assert.That(value, Is.Not.Zero);
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
                        int value = 42;
                        await Assert.That(value).IsNotZero();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_Platform_Attribute_Removed()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                public class MyClass
                {
                    {|#0:[Test]|}
                    [Platform(Include = "Win")]
                    public void TestMethod()
                    {
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
                    public void TestMethod()
                    {
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_Description_Attribute_Removed()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                public class MyClass
                {
                    {|#0:[Test]|}
                    [Description("This is a test description")]
                    public void TestMethod()
                    {
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
                    public void TestMethod()
                    {
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_Method_With_Ref_Parameter_Not_Converted_To_Async()
    {
        // Test that methods with ref parameters use .Wait() instead of await
        // Since HandleRealized has a ref parameter, it uses .Wait() and doesn't become async
        // MyTest has no assertions directly, so it doesn't become async either
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void MyTest()
                    {
                        bool realized = false;
                        HandleRealized(this, ref realized);
                    }

                    private static void HandleRealized(object sender, ref bool realized)
                    {
                        Assert.That(sender, Is.Not.Null);
                        realized = true;
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
                    public void MyTest()
                    {
                        bool realized = false;
                        HandleRealized(this, ref realized);
                    }

                    private static void HandleRealized(object sender, ref bool realized)
                    {
                        Assert.That(sender).IsNotNull().Wait();
                        realized = true;
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_Method_With_Out_Parameter_Not_Converted_To_Async()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void MyTest()
                    {
                        TryGetValue("key", out int value);
                        Assert.That(value, Is.EqualTo(42));
                    }

                    private static void TryGetValue(string key, out int value)
                    {
                        Assert.That(key, Is.Not.Null);
                        value = 42;
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
                    public async Task MyTest()
                    {
                        TryGetValue("key", out int value);
                        await Assert.That(value).IsEqualTo(42);
                    }

                    private static void TryGetValue(string key, out int value)
                    {
                        Assert.That(key).IsNotNull().Wait();
                        value = 42;
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_InterfaceImplementation_NotConvertedToAsync()
    {
        // Methods that implement interface members should NOT be converted to async
        // because that would break the interface implementation contract.
        // The interface method contains no NUnit assertions, so no await is added.
        // Only the test method (which doesn't implement an interface) gets converted to async.
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;
                using System.Threading.Tasks;

                public interface ITestRunner
                {
                    void Run();
                }

                {|#0:public class MyClass|} : ITestRunner
                {
                    [Test]
                    public void TestMethod()
                    {
                        Assert.That(true, Is.True);
                    }

                    public void Run()
                    {
                        // This implements ITestRunner.Run() and should stay void
                        var x = 1;
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

                public interface ITestRunner
                {
                    void Run();
                }

                public class MyClass : ITestRunner
                {
                    [Test]
                    public async Task TestMethod()
                    {
                        await Assert.That(true).IsTrue();
                    }

                    public void Run()
                    {
                        // This implements ITestRunner.Run() and should stay void
                        var x = 1;
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_Values_Attribute_Converted_To_Matrix()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod([Values(1, 2, 3)] int value)
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
                    public async Task TestMethod([Matrix(1, 2, 3)] int value)
                    {
                        await Assert.That(value > 0).IsTrue();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_Range_Attribute_Converted_To_MatrixRange()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod([Range(1, 5)] int value)
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
                    public async Task TestMethod([MatrixRange<int>(1, 5)] int value)
                    {
                        await Assert.That(value > 0).IsTrue();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_Range_Attribute_With_Step_Converted_To_MatrixRange()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod([Range(0, 10, 2)] int value)
                    {
                        Assert.That(value >= 0, Is.True);
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
                    public async Task TestMethod([MatrixRange<int>(0, 10, 2)] int value)
                    {
                        await Assert.That(value >= 0).IsTrue();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_Range_Attribute_With_Double_Converted_To_MatrixRange_Double()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod([Range(1.0, 5.0)] double value)
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
                    public async Task TestMethod([MatrixRange<double>(1.0, 5.0)] double value)
                    {
                        await Assert.That(value > 0).IsTrue();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_Range_Attribute_With_Long_Converted_To_MatrixRange_Long()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod([Range(1L, 100L)] long value)
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
                    public async Task TestMethod([MatrixRange<long>(1L, 100L)] long value)
                    {
                        await Assert.That(value > 0).IsTrue();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_Range_Attribute_With_Float_Converted_To_MatrixRange_Float()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod([Range(1.0f, 5.0f)] float value)
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
                    public async Task TestMethod([MatrixRange<float>(1.0f, 5.0f)] float value)
                    {
                        await Assert.That(value > 0).IsTrue();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_NonParallelizable_Attribute_Converted_To_NotInParallel()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    [NonParallelizable]
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
                    [NotInParallel]
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
    public async Task NUnit_Parallelizable_None_Converted_To_NotInParallel()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    [Parallelizable(ParallelScope.None)]
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
                    [NotInParallel]
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
    public async Task NUnit_Parallelizable_Attribute_Removed_When_Parallel_Allowed()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    [Parallelizable(ParallelScope.Self)]
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
    public async Task NUnit_Parallelizable_Attribute_Without_Args_Removed()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    [Parallelizable]
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
    public async Task NUnit_Repeat_Attribute_Preserved()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    [Repeat(5)]
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
                    [Repeat(5)]
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
    public async Task NUnit_ValueSource_Attribute_Converted_To_MatrixSourceMethod()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;
                using System.Collections.Generic;

                {|#0:public class MyClass|}
                {
                    private static IEnumerable<int> Numbers() => new[] { 1, 2, 3 };

                    [Test]
                    public void TestMethod([ValueSource(nameof(Numbers))] int value)
                    {
                        Assert.That(value > 0, Is.True);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    private static IEnumerable<int> Numbers() => new[] { 1, 2, 3 };

                    [Test]
                    public async Task TestMethod([MatrixSourceMethod(nameof(Numbers))] int value)
                    {
                        await Assert.That(value > 0).IsTrue();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_Combinatorial_Attribute_Removed()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    [Combinatorial]
                    public void TestMethod([Values(1, 2)] int a, [Values("x", "y")] string b)
                    {
                        Assert.That(a > 0, Is.True);
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
                    public async Task TestMethod([Matrix(1, 2)] int a, [Matrix("x", "y")] string b)
                    {
                        await Assert.That(a > 0).IsTrue();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_Sequential_Attribute_Removed()
    {
        // Note: [Sequential] has no direct TUnit equivalent.
        // TUnit's default behavior with Matrix is combinatorial.
        // Users may need to restructure tests using MethodDataSource for true sequential behavior.
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    [Sequential]
                    public void TestMethod([Values(1, 2)] int a, [Values("x", "y")] string b)
                    {
                        Assert.That(a > 0, Is.True);
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
                    public async Task TestMethod([Matrix(1, 2)] int a, [Matrix("x", "y")] string b)
                    {
                        await Assert.That(a > 0).IsTrue();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_FileAssert_Exists_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        FileAssert.Exists("test.txt");
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.IO;
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
                        await Assert.That(File.Exists("test.txt")).IsTrue();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_FileAssert_DoesNotExist_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        FileAssert.DoesNotExist("test.txt");
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.IO;
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
                        await Assert.That(File.Exists("test.txt")).IsFalse();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_FileAssert_AreEqual_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        FileAssert.AreEqual("expected.txt", "actual.txt");
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.IO;
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
                        await Assert.That(new FileInfo("actual.txt")).HasSameContentAs(new FileInfo("expected.txt"));
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_FileAssert_AreNotEqual_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        FileAssert.AreNotEqual("expected.txt", "actual.txt");
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.IO;
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
                        await Assert.That(new FileInfo("actual.txt")).DoesNotHaveSameContentAs(new FileInfo("expected.txt"));
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_DirectoryAssert_Exists_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        DirectoryAssert.Exists("testDir");
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.IO;
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
                        await Assert.That(Directory.Exists("testDir")).IsTrue();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_DirectoryAssert_DoesNotExist_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        DirectoryAssert.DoesNotExist("testDir");
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.IO;
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
                        await Assert.That(Directory.Exists("testDir")).IsFalse();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_DirectoryAssert_AreEqual_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        DirectoryAssert.AreEqual("expectedDir", "actualDir");
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.IO;
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
                        await Assert.That(new DirectoryInfo("actualDir")).IsEquivalentTo(new DirectoryInfo("expectedDir"));
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_ExpectedException_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using System;
                using NUnit.Framework;

                public class MyClass
                {
                    {|#0:[Test]|}
                    [ExpectedException(typeof(InvalidOperationException))]
                    public void TestMethod()
                    {
                        throw new InvalidOperationException();
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

                public class MyClass
                {
                    [Test]
                    public async Task TestMethod()
                    {
                        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                        {
                            throw new InvalidOperationException();
                        });
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_ExpectedException_With_Async_Code_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using System;
                using System.Threading.Tasks;
                using NUnit.Framework;

                public class MyClass
                {
                    {|#0:[Test]|}
                    [ExpectedException(typeof(InvalidOperationException))]
                    public async Task TestMethod()
                    {
                        await Task.Delay(1);
                        throw new InvalidOperationException();
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

                public class MyClass
                {
                    [Test]
                    public async Task TestMethod()
                    {
                        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                        {
                            await Task.Delay(1);
                            throw new InvalidOperationException();
                        });
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_Is_Ordered_Ascending_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;
                using System.Collections.Generic;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        var list = new List<int> { 1, 2, 3 };
                        Assert.That(list, Is.Ordered.Ascending);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Collections.Generic;
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
                        var list = new List<int> { 1, 2, 3 };
                        await Assert.That(list).IsInAscendingOrder();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_Is_Ordered_Descending_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;
                using System.Collections.Generic;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        var list = new List<int> { 3, 2, 1 };
                        Assert.That(list, Is.Ordered.Descending);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Collections.Generic;
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
                        var list = new List<int> { 3, 2, 1 };
                        await Assert.That(list).IsInDescendingOrder();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_Is_Ordered_Without_Direction_Defaults_To_Ascending()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;
                using System.Collections.Generic;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        var list = new List<int> { 1, 2, 3 };
                        Assert.That(list, Is.Ordered);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Collections.Generic;
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
                        var list = new List<int> { 1, 2, 3 };
                        await Assert.That(list).IsInAscendingOrder();
                    }
                }
                """,
            ConfigureNUnitTest
        );
    }

    [Test]
    public async Task NUnit_Is_Unique_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using NUnit.Framework;
                using System.Collections.Generic;

                {|#0:public class MyClass|}
                {
                    [Test]
                    public void TestMethod()
                    {
                        var list = new List<int> { 1, 2, 3 };
                        Assert.That(list, Is.Unique);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.NUnitMigration).WithLocation(0),
            """
                using System.Collections.Generic;
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
                        var list = new List<int> { 1, 2, 3 };
                        await Assert.That(list).HasDistinctItems();
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
