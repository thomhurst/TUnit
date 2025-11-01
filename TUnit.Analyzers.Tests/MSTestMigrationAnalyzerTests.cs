using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeFixer = TUnit.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<TUnit.Analyzers.MSTestMigrationAnalyzer, TUnit.Analyzers.CodeFixers.MSTestMigrationCodeFixProvider>;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.MSTestMigrationAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class MSTestMigrationAnalyzerTests
{
    [Test]
    [Arguments("Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod")]
    [Arguments("Microsoft.VisualStudio.TestTools.UnitTesting.DataRow")]
    public async Task MSTest_Attribute_Flagged(string attribute)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
                using Microsoft.VisualStudio.TestTools.UnitTesting;

                public class MyClass
                {
                    {|#0:[{{attribute}}]|}
                    public void MyMethod() { }
                }
                """,
            ConfigureMSTestTest,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0)
        );
    }

    [Test]
    [Arguments("Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod", "Test")]
    [Arguments("Microsoft.VisualStudio.TestTools.UnitTesting.DataRow(1, 2, 3)", "Arguments(1, 2, 3)")]
    [Arguments("Microsoft.VisualStudio.TestTools.UnitTesting.TestInitialize", "Before(HookType.Test)")]
    [Arguments("Microsoft.VisualStudio.TestTools.UnitTesting.TestCleanup", "After(HookType.Test)")]
    [Arguments("Microsoft.VisualStudio.TestTools.UnitTesting.DynamicData(\"SomeMethod\")", "MethodDataSource(\"SomeMethod\")")]
    public async Task MSTest_Attribute_Can_Be_Converted(string attribute, string expected)
    {
        await CodeFixer.VerifyCodeFixAsync(
            $$"""
                using Microsoft.VisualStudio.TestTools.UnitTesting;

                public class MyClass
                {
                    {|#0:[{{attribute}}]|}
                    public void MyMethod() { }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
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
            ConfigureMSTestTest
        );
    }

    [Test]
    public async Task MSTest_TestClass_Attribute_Removed()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using Microsoft.VisualStudio.TestTools.UnitTesting;

                {|#0:[TestClass]|}
                public class MyClass
                {
                    [TestMethod]
                    public void MyMethod() { }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
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
            ConfigureMSTestTest
        );
    }

    [Test]
    public async Task MSTest_Assertions_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using Microsoft.VisualStudio.TestTools.UnitTesting;

                {|#0:public class MyClass|}
                {
                    [TestMethod]
                    public void MyMethod()
                    {
                        Assert.AreEqual(5, 5);
                        Assert.IsTrue(true);
                        Assert.IsNull(null);
                        Assert.AreNotEqual(3, 5);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
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
                        await Assert.That(5).IsEqualTo(5);
                        await Assert.That(true).IsTrue();
                        await Assert.That(null).IsNull();
                        await Assert.That(5).IsNotEqualTo(3);
                    }
                }
                """,
            ConfigureMSTestTest
        );
    }

    [Test]
    public async Task MSTest_Directive_Flagged()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
                {|#0:using Microsoft.VisualStudio.TestTools.UnitTesting;|}

                public class MyClass
                {
                    public void MyMethod() { }
                }
                """,
            ConfigureMSTestTest,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0)
        );
    }

    [Test]
    public async Task MSTest_Directive_Can_Be_Removed()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                {|#0:using Microsoft.VisualStudio.TestTools.UnitTesting;|}

                public class MyClass
                {
                    public void MyMethod() { }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
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
            ConfigureMSTestTest
        );
    }

    [Test]
    public async Task MSTest_TestInitialize_TestCleanup_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using Microsoft.VisualStudio.TestTools.UnitTesting;

                {|#0:public class MyClass|}
                {
                    [TestInitialize]
                    public void Setup() { }

                    [TestCleanup]
                    public void Teardown() { }

                    [TestMethod]
                    public void MyMethod() { }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
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
            ConfigureMSTestTest
        );
    }

    [Test]
    public async Task MSTest_ClassInitialize_ClassCleanup_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using Microsoft.VisualStudio.TestTools.UnitTesting;

                {|#0:public class MyClass|}
                {
                    [ClassInitialize]
                    public static void ClassSetup(TestContext context) { }

                    [ClassCleanup]
                    public static void ClassTeardown() { }

                    [TestMethod]
                    public void MyMethod() { }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
            """
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Before(HookType.Class)]
                    public static void ClassSetup() { }

                    [After(HookType.Class)]
                    public static void ClassTeardown() { }

                    [Test]
                    public void MyMethod() { }
                }
                """,
            ConfigureMSTestTest
        );
    }

    [Test]
    public async Task MSTest_CollectionAssert_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using Microsoft.VisualStudio.TestTools.UnitTesting;

                {|#0:public class MyClass|}
                {
                    [TestMethod]
                    public void MyMethod()
                    {
                        var list1 = new[] { 1, 2, 3 };
                        var list2 = new[] { 1, 2, 3 };
                        CollectionAssert.AreEqual(list1, list2);
                        CollectionAssert.Contains(list1, 2);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
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
                        var list1 = new[] { 1, 2, 3 };
                        var list2 = new[] { 1, 2, 3 };
                        await Assert.That(list2).IsEquivalentTo(list1);
                        await Assert.That(list1).Contains(2);
                    }
                }
                """,
            ConfigureMSTestTest
        );
    }

    [Test]
    public async Task MSTest_StringAssert_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using Microsoft.VisualStudio.TestTools.UnitTesting;

                {|#0:public class MyClass|}
                {
                    [TestMethod]
                    public void StringTests()
                    {
                        StringAssert.Contains("hello world", "world");
                        StringAssert.StartsWith("hello world", "hello");
                        StringAssert.EndsWith("hello world", "world");
                    }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
            """
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    public void StringTests()
                    {
                        await Assert.That("hello world").Contains("world");
                        await Assert.That("hello world").StartsWith("hello");
                        await Assert.That("hello world").EndsWith("world");
                    }
                }
                """,
            ConfigureMSTestTest
        );
    }

    [Test]
    public async Task MSTest_Nested_Class_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using Microsoft.VisualStudio.TestTools.UnitTesting;

                {|#0:public class OuterClass|}
                {
                    public class InnerTests
                    {
                        [TestMethod]
                        public void InnerTest()
                        {
                            Assert.IsTrue(true);
                        }
                    }

                    [TestMethod]
                    public void OuterTest()
                    {
                        Assert.IsFalse(false);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
            """
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class OuterClass
                {
                    public class InnerTests
                    {
                        [Test]
                        public void InnerTest()
                        {
                            await Assert.That(true).IsTrue();
                        }
                    }

                    [Test]
                    public void OuterTest()
                    {
                        await Assert.That(false).IsFalse();
                    }
                }
                """,
            ConfigureMSTestTest
        );
    }

    [Test]
    public async Task MSTest_Generic_Test_Class_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using Microsoft.VisualStudio.TestTools.UnitTesting;

                {|#0:public class GenericTestClass|}<T>
                {
                    [TestMethod]
                    public void GenericTest()
                    {
                        var instance = default(T);
                        Assert.AreEqual(default(T), instance);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
            """
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class GenericTestClass<T>
                {
                    [Test]
                    public void GenericTest()
                    {
                        var instance = default(T);
                        await Assert.That(instance).IsEqualTo(default(T));
                    }
                }
                """,
            ConfigureMSTestTest
        );
    }

    [Test]
    public async Task MSTest_Complete_File_Transformation()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using Microsoft.VisualStudio.TestTools.UnitTesting;
                using System;

                {|#0:[TestClass]|}
                public class CompleteTestClass
                {
                    private int _counter;

                    [ClassInitialize]
                    public static void ClassSetup(TestContext context)
                    {
                        // Class setup
                    }

                    [TestInitialize]
                    public void Setup()
                    {
                        _counter = 1;
                    }

                    [TestMethod]
                    public void Test1()
                    {
                        Assert.IsTrue(_counter > 0);
                        Assert.IsNotNull(_counter);
                    }

                    [DataRow(1, 2, 3)]
                    [DataRow(5, 5, 10)]
                    [TestMethod]
                    public void AdditionTest(int a, int b, int expected)
                    {
                        var result = a + b;
                        Assert.AreEqual(expected, result);
                    }

                    [DynamicData(nameof(GetTestData))]
                    [TestMethod]
                    public void DataDrivenTest(string input)
                    {
                        Assert.IsNotNull(input);
                    }

                    public static System.Collections.Generic.IEnumerable<object[]> GetTestData()
                    {
                        yield return new object[] { "test1" };
                        yield return new object[] { "test2" };
                    }

                    [TestCleanup]
                    public void Teardown()
                    {
                        // Cleanup
                    }

                    [ClassCleanup]
                    public static void ClassTeardown()
                    {
                        // Class cleanup
                    }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
            """
                using System;
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class CompleteTestClass
                {
                    private int _counter;

                    [Before(HookType.Class)]
                    public static void ClassSetup()
                    {
                        // Class setup
                    }

                    [Before(HookType.Test)]
                    public void Setup()
                    {
                        _counter = 1;
                    }

                    [Test]
                    public void Test1()
                    {
                        await Assert.That(_counter > 0).IsTrue();
                        await Assert.That(_counter).IsNotNull();
                    }

                    [Arguments(1, 2, 3)]
                    [Arguments(5, 5, 10)]
                    [Test]
                    public void AdditionTest(int a, int b, int expected)
                    {
                        var result = a + b;
                        await Assert.That(result).IsEqualTo(expected);
                    }

                    [MethodDataSource(nameof(GetTestData))]
                    [Test]
                    public void DataDrivenTest(string input)
                    {
                        await Assert.That(input).IsNotNull();
                    }

                    public static System.Collections.Generic.IEnumerable<object[]> GetTestData()
                    {
                        yield return new object[] { "test1" };
                        yield return new object[] { "test2" };
                    }

                    [After(HookType.Test)]
                    public void Teardown()
                    {
                        // Cleanup
                    }

                    [After(HookType.Class)]
                    public static void ClassTeardown()
                    {
                        // Class cleanup
                    }
                }
                """,
            ConfigureMSTestTest
        );
    }

    [Test]
    public async Task MSTest_Multiple_Assertion_Types()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using Microsoft.VisualStudio.TestTools.UnitTesting;

                {|#0:public class MyClass|}
                {
                    [TestMethod]
                    public void TestMultipleAssertionTypes()
                    {
                        var value = 42;
                        var list = new[] { 1, 2, 3 };
                        var text = "hello";

                        // Standard assertions
                        Assert.AreEqual(42, value);
                        Assert.IsNotNull(value);
                        Assert.IsTrue(value > 0);

                        // Collection assertions
                        CollectionAssert.Contains(list, 2);
                        CollectionAssert.AreNotEqual(list, new[] { 4, 5, 6 });

                        // String assertions
                        StringAssert.Contains(text, "ell");
                        StringAssert.StartsWith(text, "hel");
                    }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
            """
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    public void TestMultipleAssertionTypes()
                    {
                        var value = 42;
                        var list = new[] { 1, 2, 3 };
                        var text = "hello";

                        // Standard assertions
                        await Assert.That(value).IsEqualTo(42);
                        await Assert.That(value).IsNotNull();
                        await Assert.That(value > 0).IsTrue();

                        // Collection assertions
                        await Assert.That(list).Contains(2);
                        await Assert.That(new[] { 4, 5, 6 }).IsNotEquivalentTo(list);

                        // String assertions
                        await Assert.That(text).Contains("ell");
                        await Assert.That(text).StartsWith("hel");
                    }
                }
                """,
            ConfigureMSTestTest
        );
    }

    [Test]
    public async Task MSTest_Reference_Type_Assertions()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using Microsoft.VisualStudio.TestTools.UnitTesting;

                {|#0:public class MyClass|}
                {
                    [TestMethod]
                    public void TestReferences()
                    {
                        var obj1 = new object();
                        var obj2 = obj1;
                        var obj3 = new object();

                        Assert.AreSame(obj1, obj2);
                        Assert.AreNotSame(obj1, obj3);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
            """
                using TUnit.Core;
                using TUnit.Assertions;
                using static TUnit.Assertions.Assert;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    [Test]
                    public void TestReferences()
                    {
                        var obj1 = new object();
                        var obj2 = obj1;
                        var obj3 = new object();

                        await Assert.That(obj2).IsSameReference(obj1);
                        await Assert.That(obj3).IsNotSameReference(obj1);
                    }
                }
                """,
            ConfigureMSTestTest
        );
    }

    private static void ConfigureMSTestTest(Verifier.Test test)
    {
        test.TestState.AdditionalReferences.Add(typeof(TestMethodAttribute).Assembly);
    }

    private static void ConfigureMSTestTest(CodeFixer.Test test)
    {
        test.TestState.AdditionalReferences.Add(typeof(TestMethodAttribute).Assembly);
        // FixedState should only have TUnit assemblies, not MSTest
        test.FixedState.AdditionalReferences.Add(typeof(TUnit.Core.TestAttribute).Assembly);
        test.FixedState.AdditionalReferences.Add(typeof(TUnit.Assertions.Assert).Assembly);
    }
}
