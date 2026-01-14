using Microsoft.CodeAnalysis.Testing;
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
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task MyMethod()
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
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task MyMethod()
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
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task StringTests()
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
                using System.Threading.Tasks;

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
                using System.Threading.Tasks;

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
                using System.Threading.Tasks;

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
                    public async Task Test1()
                    {
                        await Assert.That(_counter > 0).IsTrue();
                        await Assert.That(_counter).IsNotNull();
                    }

                    [Arguments(1, 2, 3)]
                    [Arguments(5, 5, 10)]
                    [Test]
                    public async Task AdditionTest(int a, int b, int expected)
                    {
                        var result = a + b;
                        await Assert.That(result).IsEqualTo(expected);
                    }

                    [MethodDataSource(nameof(GetTestData))]
                    [Test]
                    public async Task DataDrivenTest(string input)
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
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task TestMultipleAssertionTypes()
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
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task TestReferences()
                    {
                        var obj1 = new object();
                        var obj2 = obj1;
                        var obj3 = new object();

                        await Assert.That(obj2).IsSameReferenceAs(obj1);
                        await Assert.That(obj3).IsNotSameReferenceAs(obj1);
                    }
                }
                """,
            ConfigureMSTestTest
        );
    }

    [Test]
    public async Task MSTest_Assertion_Messages_Preserved()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using Microsoft.VisualStudio.TestTools.UnitTesting;

                {|#0:public class MyClass|}
                {
                    [TestMethod]
                    public void TestWithMessages()
                    {
                        Assert.AreEqual(5, 5, "Values should be equal");
                        Assert.IsTrue(true, "Should be true");
                        Assert.IsNull(null, "Should be null");
                        Assert.AreNotEqual(3, 5, "Values should not be equal");
                    }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
            """
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task TestWithMessages()
                    {
                        await Assert.That(5).IsEqualTo(5).Because("Values should be equal");
                        await Assert.That(true).IsTrue().Because("Should be true");
                        await Assert.That(null).IsNull().Because("Should be null");
                        await Assert.That(5).IsNotEqualTo(3).Because("Values should not be equal");
                    }
                }
                """,
            ConfigureMSTestTest
        );
    }

    [Test]
    public async Task MSTest_Assertions_With_FormatStrings_Converted()
    {
        // Note: The diagnostic is on [TestMethod] because Assert.AreEqual with format strings
        // isn't a valid MSTest overload, so semantic model doesn't resolve it.
        // The analyzer detects the method attribute instead of the Assert call.
        await CodeFixer.VerifyCodeFixAsync(
            """
                using Microsoft.VisualStudio.TestTools.UnitTesting;

                public class MyClass
                {
                    {|#0:[TestMethod]|}
                    public void TestWithFormatStrings()
                    {
                        int x = 5;
                        Assert.AreEqual(5, x, "Expected {0} but got {1}", 5, x);
                        Assert.AreNotEqual(3, x, "Values should differ: {0}", x);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
            """
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task TestWithFormatStrings()
                    {
                        int x = 5;
                        await Assert.That(x).IsEqualTo(5).Because(string.Format("Expected {0} but got {1}", 5, x));
                        await Assert.That(x).IsNotEqualTo(3).Because(string.Format("Values should differ: {0}", x));
                    }
                }
                """,
            ConfigureMSTestTest
        );
    }

    [Test]
    public async Task MSTest_Assertions_With_Comparer_AddsTodoComment()
    {
        // When a comparer is detected (via semantic or syntax-based detection),
        // a TODO comment is added explaining that TUnit uses different comparison semantics.
        // Note: The diagnostic is on [TestMethod] because Assert.AreEqual with comparer
        // isn't a valid MSTest overload, so semantic model doesn't resolve it.
        await CodeFixer.VerifyCodeFixAsync(
            """
                using Microsoft.VisualStudio.TestTools.UnitTesting;
                using System.Collections.Generic;

                public class MyClass
                {
                    {|#0:[TestMethod]|}
                    public void TestWithComparer()
                    {
                        var comparer = StringComparer.OrdinalIgnoreCase;
                        Assert.AreEqual("hello", "HELLO", comparer);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
            """
                using System.Collections.Generic;
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task TestWithComparer()
                    {
                        var comparer = StringComparer.OrdinalIgnoreCase;
                        // TODO: TUnit migration - IEqualityComparer was used. TUnit uses .IsEqualTo() which may have different comparison semantics.
                        await Assert.That("HELLO").IsEqualTo("hello");
                    }
                }
                """,
            ConfigureMSTestTest
        );
    }

    [Test]
    public async Task MSTest_Priority_Attribute_Converted_To_Property()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using Microsoft.VisualStudio.TestTools.UnitTesting;

                public class MyClass
                {
                    {|#0:[TestMethod]|}
                    [Priority(1)]
                    public void TestMethod()
                    {
                    }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
            """

                public class MyClass
                {
                    [Test]
                    [Property("Priority", "1")]
                    public void TestMethod()
                    {
                    }
                }
                """,
            ConfigureMSTestTest
        );
    }

    [Test]
    public async Task MSTest_TestCategory_Attribute_Converted_To_Property()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using Microsoft.VisualStudio.TestTools.UnitTesting;

                public class MyClass
                {
                    {|#0:[TestMethod]|}
                    [TestCategory("Integration")]
                    public void TestMethod()
                    {
                    }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
            """

                public class MyClass
                {
                    [Test]
                    [Property("Category", "Integration")]
                    public void TestMethod()
                    {
                    }
                }
                """,
            ConfigureMSTestTest
        );
    }

    [Test]
    public async Task MSTest_Owner_Attribute_Converted_To_Property()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using Microsoft.VisualStudio.TestTools.UnitTesting;

                public class MyClass
                {
                    {|#0:[TestMethod]|}
                    [Owner("JohnDoe")]
                    public void TestMethod()
                    {
                    }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
            """

                public class MyClass
                {
                    [Test]
                    [Property("Owner", "JohnDoe")]
                    public void TestMethod()
                    {
                    }
                }
                """,
            ConfigureMSTestTest
        );
    }

    [Test]
    public async Task MSTest_ExpectedException_Attribute_Converted_To_ThrowsAsync()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using System;
                using Microsoft.VisualStudio.TestTools.UnitTesting;

                public class MyClass
                {
                    {|#0:[TestMethod]|}
                    [ExpectedException(typeof(ArgumentException))]
                    public void TestMethod()
                    {
                        throw new ArgumentException("test");
                    }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
            """
                using System;
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task TestMethod()
                    {
                        await Assert.ThrowsAsync<ArgumentException>(() =>
                        {
                            throw new ArgumentException("test");
                        });
                    }
                }
                """,
            ConfigureMSTestTest
        );
    }

    [Test]
    public async Task MSTest_ExpectedException_Attribute_On_Async_Method_Converted_To_ThrowsAsync()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using System;
                using System.Threading.Tasks;
                using Microsoft.VisualStudio.TestTools.UnitTesting;

                public class MyClass
                {
                    {|#0:[TestMethod]|}
                    [ExpectedException(typeof(ArgumentException))]
                    public async Task TestMethodAsync()
                    {
                        await Task.Delay(1);
                        throw new ArgumentException("test");
                    }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
            """
                using System;
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task TestMethodAsync()
                    {
                        await Assert.ThrowsAsync<ArgumentException>(async () =>
                        {
                            await Task.Delay(1);
                            throw new ArgumentException("test");
                        });
                    }
                }
                """,
            ConfigureMSTestTest
        );
    }

    [Test]
    public async Task MSTest_DirectoryAssert_Exists_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using Microsoft.VisualStudio.TestTools.UnitTesting;
                using System.IO;

                {|#0:[TestClass]|}
                public class MyClass
                {
                    [TestMethod]
                    public void TestMethod()
                    {
                        var dir = new DirectoryInfo("C:/temp");
                        DirectoryAssert.Exists(dir);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
            """
                using System.IO;
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task TestMethod()
                    {
                        var dir = new DirectoryInfo("C:/temp");
                        await Assert.That(dir.Exists).IsTrue();
                    }
                }
                """,
            ConfigureMSTestTest
        );
    }

    [Test]
    public async Task MSTest_DirectoryAssert_DoesNotExist_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using Microsoft.VisualStudio.TestTools.UnitTesting;
                using System.IO;

                {|#0:[TestClass]|}
                public class MyClass
                {
                    [TestMethod]
                    public void TestMethod()
                    {
                        var dir = new DirectoryInfo("C:/nonexistent");
                        DirectoryAssert.DoesNotExist(dir);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
            """
                using System.IO;
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task TestMethod()
                    {
                        var dir = new DirectoryInfo("C:/nonexistent");
                        await Assert.That(dir.Exists).IsFalse();
                    }
                }
                """,
            ConfigureMSTestTest
        );
    }

    [Test]
    public async Task MSTest_FileAssert_Exists_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using Microsoft.VisualStudio.TestTools.UnitTesting;
                using System.IO;

                {|#0:[TestClass]|}
                public class MyClass
                {
                    [TestMethod]
                    public void TestMethod()
                    {
                        var file = new FileInfo("C:/temp/file.txt");
                        FileAssert.Exists(file);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
            """
                using System.IO;
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task TestMethod()
                    {
                        var file = new FileInfo("C:/temp/file.txt");
                        await Assert.That(file.Exists).IsTrue();
                    }
                }
                """,
            ConfigureMSTestTest
        );
    }

    [Test]
    public async Task MSTest_FileAssert_DoesNotExist_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using Microsoft.VisualStudio.TestTools.UnitTesting;
                using System.IO;

                {|#0:[TestClass]|}
                public class MyClass
                {
                    [TestMethod]
                    public void TestMethod()
                    {
                        var file = new FileInfo("C:/temp/nonexistent.txt");
                        FileAssert.DoesNotExist(file);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
            """
                using System.IO;
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task TestMethod()
                    {
                        var file = new FileInfo("C:/temp/nonexistent.txt");
                        await Assert.That(file.Exists).IsFalse();
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
        // Add MSTest assemblies to TestState (for input code compilation)
        test.TestState.AdditionalReferences.Add(typeof(TestMethodAttribute).Assembly);

        // FixedState: TUnit assemblies only (NO MSTest inheritance)
        // Use Explicit inheritance mode to prevent MSTest references from being inherited
        // This ensures the analyzer's IsFrameworkAvailable check returns false for MSTest
        test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
        test.FixedState.AdditionalReferences.Add(typeof(TUnit.Core.TestAttribute).Assembly);
        test.FixedState.AdditionalReferences.Add(typeof(TUnit.Assertions.Assert).Assembly);

        // With Explicit mode, we need to copy AnalyzerConfigFiles from TestState
        // The .editorconfig is added by CSharpCodeFixVerifier base class
        test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", SourceText.From("""
            is_global = true
            end_of_line = lf
            """)));
    }
}
