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

    [Test]
    public async Task MSTest_KitchenSink_Comprehensive_Migration()
    {
        // This test combines MANY MSTest patterns together to ensure the code fixer
        // can handle complex real-world scenarios in a single pass.
        await CodeFixer.VerifyCodeFixAsync(
            """
                using Microsoft.VisualStudio.TestTools.UnitTesting;
                using System;
                using System.Collections.Generic;

                {|#0:[TestClass]|}
                public class KitchenSinkTests
                {
                    private static List<string> _log;
                    private int _counter;

                    [ClassInitialize]
                    public static void ClassSetup(TestContext context)
                    {
                        _log = new List<string>();
                    }

                    [TestInitialize]
                    public void TestSetup()
                    {
                        _counter = 0;
                    }

                    [TestMethod]
                    public void BasicTest()
                    {
                        Assert.IsTrue(_counter == 0);
                        Assert.AreEqual(0, _counter);
                        Assert.IsNotNull(_log);
                    }

                    [TestMethod]
                    [DataRow(1, 2, 3)]
                    [DataRow(10, 20, 30)]
                    [DataRow(-1, 1, 0)]
                    public void ParameterizedTest(int a, int b, int expected)
                    {
                        var result = a + b;
                        Assert.AreEqual(expected, result);
                    }

                    [TestMethod]
                    [DynamicData(nameof(GetTestData))]
                    public void DataSourceTest(string input, int expectedLength)
                    {
                        Assert.AreEqual(expectedLength, input.Length);
                        Assert.IsNotNull(input);
                    }

                    public static IEnumerable<object[]> GetTestData()
                    {
                        yield return new object[] { "hello", 5 };
                        yield return new object[] { "world", 5 };
                    }

                    [TestMethod]
                    public void CollectionAssertTest()
                    {
                        var list = new List<int> { 1, 2, 3 };
                        CollectionAssert.Contains(list, 2);
                        CollectionAssert.AllItemsAreUnique(list);
                    }

                    [TestMethod]
                    public void StringAssertTest()
                    {
                        var text = "Hello World";
                        StringAssert.Contains(text, "World");
                        StringAssert.StartsWith(text, "Hello");
                        StringAssert.EndsWith(text, "World");
                    }

                    [TestMethod]
                    public void ExceptionTest()
                    {
                        Assert.ThrowsException<ArgumentException>(() => throw new ArgumentException("test"));
                    }

                    [TestMethod]
                    public void ComparisonAssertions()
                    {
                        var value = 42;
                        Assert.IsTrue(value > 0);
                        Assert.IsFalse(value < 0);
                        Assert.AreNotEqual(0, value);
                    }

                    [TestMethod]
                    public void NullAssertions()
                    {
                        string? nullValue = null;
                        var notNullValue = "test";
                        Assert.IsNull(nullValue);
                        Assert.IsNotNull(notNullValue);
                    }

                    [TestMethod]
                    public void TypeAssertions()
                    {
                        object obj = "test string";
                        Assert.IsInstanceOfType(obj, typeof(string));
                    }

                    [TestCleanup]
                    public void TestTeardown()
                    {
                        _counter = 0;
                    }

                    [ClassCleanup]
                    public static void ClassTeardown()
                    {
                        _log.Clear();
                    }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
            """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;

                public class KitchenSinkTests
                {
                    private static List<string> _log;
                    private int _counter;

                    [Before(HookType.Class)]
                    public static void ClassSetup()
                    {
                        _log = new List<string>();
                    }

                    [Before(HookType.Test)]
                    public void TestSetup()
                    {
                        _counter = 0;
                    }

                    [Test]
                    public async Task BasicTest()
                    {
                        await Assert.That(_counter == 0).IsTrue();
                        await Assert.That(_counter).IsEqualTo(0);
                        await Assert.That(_log).IsNotNull();
                    }

                    [Test]
                    [Arguments(1, 2, 3)]
                    [Arguments(10, 20, 30)]
                    [Arguments(-1, 1, 0)]
                    public async Task ParameterizedTest(int a, int b, int expected)
                    {
                        var result = a + b;
                        await Assert.That(result).IsEqualTo(expected);
                    }

                    [Test]
                    [MethodDataSource(nameof(GetTestData))]
                    public async Task DataSourceTest(string input, int expectedLength)
                    {
                        await Assert.That(input.Length).IsEqualTo(expectedLength);
                        await Assert.That(input).IsNotNull();
                    }

                    public static IEnumerable<object[]> GetTestData()
                    {
                        yield return new object[] { "hello", 5 };
                        yield return new object[] { "world", 5 };
                    }

                    [Test]
                    public async Task CollectionAssertTest()
                    {
                        var list = new List<int> { 1, 2, 3 };
                        await Assert.That(list).Contains(2);
                        await Assert.That(list).HasDistinctItems();
                    }

                    [Test]
                    public async Task StringAssertTest()
                    {
                        var text = "Hello World";
                        await Assert.That(text).Contains("World");
                        await Assert.That(text).StartsWith("Hello");
                        await Assert.That(text).EndsWith("World");
                    }

                    [Test]
                    public async Task ExceptionTest()
                    {
                        await Assert.ThrowsAsync<ArgumentException>(() => throw new ArgumentException("test"));
                    }

                    [Test]
                    public async Task ComparisonAssertions()
                    {
                        var value = 42;
                        await Assert.That(value > 0).IsTrue();
                        await Assert.That(value < 0).IsFalse();
                        await Assert.That(value).IsNotEqualTo(0);
                    }

                    [Test]
                    public async Task NullAssertions()
                    {
                        string? nullValue = null;
                        var notNullValue = "test";
                        await Assert.That(nullValue).IsNull();
                        await Assert.That(notNullValue).IsNotNull();
                    }

                    [Test]
                    public async Task TypeAssertions()
                    {
                        object obj = "test string";
                        await Assert.That(obj).IsAssignableTo(typeof(string));
                    }

                    [After(HookType.Test)]
                    public void TestTeardown()
                    {
                        _counter = 0;
                    }

                    [After(HookType.Class)]
                    public static void ClassTeardown()
                    {
                        _log.Clear();
                    }
                }
                """,
            ConfigureMSTestTest
        );
    }

    /// <summary>
    /// Tests that ClassInitialize/ClassCleanup with sibling attributes preserve all attributes.
    /// Bug fix: Early return in VisitAttributeList was losing sibling attributes.
    /// </summary>
    [Test]
    public async Task MSTest_ClassLifecycle_With_Sibling_Attributes_Preserved()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using Microsoft.VisualStudio.TestTools.UnitTesting;

                {|#0:[TestClass]|}
                public class TestClass
                {
                    [ClassInitialize, Description("Class setup")]
                    public static void ClassSetup(TestContext context)
                    {
                    }

                    [ClassCleanup, Description("Class teardown")]
                    public static void ClassTeardown()
                    {
                    }

                    [TestInitialize, Description("Test setup")]
                    public void TestSetup()
                    {
                    }

                    [TestCleanup, Description("Test teardown")]
                    public void TestTeardown()
                    {
                    }

                    [TestMethod]
                    public void MyTest()
                    {
                    }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
            """
                public class TestClass
                {
                    [Before(HookType.Class), Description("Class setup")]
                    public static void ClassSetup()
                    {
                    }

                    [After(HookType.Class), Description("Class teardown")]
                    public static void ClassTeardown()
                    {
                    }

                    [Before(HookType.Test), Description("Test setup")]
                    public void TestSetup()
                    {
                    }

                    [After(HookType.Test), Description("Test teardown")]
                    public void TestTeardown()
                    {
                    }

                    [Test]
                    public void MyTest()
                    {
                    }
                }
                """,
            ConfigureMSTestTest
        );
    }

    /// <summary>
    /// Tests that multiple classes in a single file are all processed correctly.
    /// Bug fix: Trivia cleanup was failing on second class due to stale node references.
    /// </summary>
    [Test]
    public async Task MSTest_Multiple_Classes_In_File_All_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using Microsoft.VisualStudio.TestTools.UnitTesting;

                {|#0:[TestClass]|}
                public class FirstTestClass
                {
                    [TestMethod]
                    public void FirstTest()
                    {
                        Assert.IsTrue(true);
                    }
                }

                [TestClass]
                public class SecondTestClass
                {
                    [TestMethod]
                    public void SecondTest()
                    {
                        Assert.IsFalse(false);
                    }
                }

                [TestClass]
                public class ThirdTestClass
                {
                    [TestInitialize]
                    public void Setup()
                    {
                    }

                    [TestMethod]
                    public void ThirdTest()
                    {
                        Assert.AreEqual(1, 1);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
            """
                using System.Threading.Tasks;

                public class FirstTestClass
                {
                    [Test]
                    public async Task FirstTest()
                    {
                        await Assert.That(true).IsTrue();
                    }
                }
                public class SecondTestClass
                {
                    [Test]
                    public async Task SecondTest()
                    {
                        await Assert.That(false).IsFalse();
                    }
                }
                public class ThirdTestClass
                {
                    [Before(HookType.Test)]
                    public void Setup()
                    {
                    }

                    [Test]
                    public async Task ThirdTest()
                    {
                        await Assert.That(1).IsEqualTo(1);
                    }
                }
                """,
            ConfigureMSTestTest
        );
    }

    [Test]
    public async Task MSTest_Comprehensive_Kitchen_Sink_Migration()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using Microsoft.VisualStudio.TestTools.UnitTesting;
                using System;
                using System.Collections.Generic;

                {|#0:[TestClass]|}
                [Description("Comprehensive MSTest fixture for migration")]
                [TestCategory("Migration")]
                public class ComprehensiveTests
                {
                    private List<string> _log;
                    private int _counter;

                    [ClassInitialize]
                    public static void ClassSetup(TestContext context)
                    {
                        // Class-level setup
                    }

                    [ClassCleanup]
                    public static void ClassTeardown()
                    {
                        // Class-level teardown
                    }

                    [TestInitialize]
                    public void TestSetup()
                    {
                        _log = new List<string>();
                        _counter = 0;
                    }

                    [TestCleanup]
                    public void TestTeardown()
                    {
                        _log.Clear();
                        _counter = -1;
                    }

                    [TestMethod]
                    [Description("Simple test without parameters")]
                    public void SimpleTest()
                    {
                        Assert.IsTrue(true);
                        Assert.IsFalse(false);
                    }

                    [TestMethod]
                    [TestCategory("Math")]
                    public void MathTest()
                    {
                        var result = 2 + 2;
                        Assert.AreEqual(4, result);
                        Assert.AreNotEqual(5, result);
                    }

                    [TestMethod]
                    [DataRow(1, 2, 3)]
                    [DataRow(5, 5, 10)]
                    [DataRow(-1, 1, 0)]
                    [Description("Parameterized addition test")]
                    public void AdditionTest(int a, int b, int expected)
                    {
                        var result = a + b;
                        Assert.AreEqual(expected, result);
                    }

                    [TestMethod]
                    [DataRow("hello", 5)]
                    [DataRow("world", 5)]
                    public void StringLengthTest(string input, int expectedLength)
                    {
                        Assert.AreEqual(expectedLength, input.Length);
                        Assert.IsNotNull(input);
                    }

                    [TestMethod]
                    [TestCategory("Null")]
                    public void NullAndTypeTests()
                    {
                        object obj = "test";
                        object nullObj = null;

                        Assert.IsNotNull(obj);
                        Assert.IsNull(nullObj);
                        Assert.IsInstanceOfType(obj, typeof(string));
                    }

                    [TestMethod]
                    public void CollectionTests()
                    {
                        var list = new List<int> { 1, 2, 3 };
                        var empty = new List<int>();

                        CollectionAssert.Contains(list, 2);
                        CollectionAssert.DoesNotContain(list, 4);
                    }

                    [TestMethod]
                    public void StringTests()
                    {
                        var str = "Hello World";

                        StringAssert.StartsWith(str, "Hello");
                        StringAssert.EndsWith(str, "World");
                        StringAssert.Contains(str, "lo Wo");
                    }

                    [TestMethod]
                    [Ignore("This test is temporarily disabled")]
                    public void IgnoredTest()
                    {
                        Assert.Fail("Should not run");
                    }
                }

                [TestClass]
                [TestCategory("Secondary")]
                public class SecondaryTests
                {
                    [TestMethod]
                    public void AnotherTest()
                    {
                        Assert.AreEqual(1, 1);
                    }

                    [TestMethod]
                    [DataRow(true)]
                    [DataRow(false)]
                    public void BooleanTest(bool value)
                    {
                        Assert.AreEqual(value, value);
                    }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
            """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;

                [Description("Comprehensive MSTest fixture for migration")]
                [Property("Category", "Migration")]
                public class ComprehensiveTests
                {
                    private List<string> _log;
                    private int _counter;

                    [Before(HookType.Class)]
                    public static void ClassSetup()
                    {
                        // Class-level setup
                    }

                    [After(HookType.Class)]
                    public static void ClassTeardown()
                    {
                        // Class-level teardown
                    }

                    [Before(HookType.Test)]
                    public void TestSetup()
                    {
                        _log = new List<string>();
                        _counter = 0;
                    }

                    [After(HookType.Test)]
                    public void TestTeardown()
                    {
                        _log.Clear();
                        _counter = -1;
                    }

                    [Test]
                    [Description("Simple test without parameters")]
                    public async Task SimpleTest()
                    {
                        await Assert.That(true).IsTrue();
                        await Assert.That(false).IsFalse();
                    }

                    [Test]
                    [Property("Category", "Math")]
                    public async Task MathTest()
                    {
                        var result = 2 + 2;
                        await Assert.That(result).IsEqualTo(4);
                        await Assert.That(result).IsNotEqualTo(5);
                    }

                    [Test]
                    [Arguments(1, 2, 3)]
                    [Arguments(5, 5, 10)]
                    [Arguments(-1, 1, 0)]
                    [Description("Parameterized addition test")]
                    public async Task AdditionTest(int a, int b, int expected)
                    {
                        var result = a + b;
                        await Assert.That(result).IsEqualTo(expected);
                    }

                    [Test]
                    [Arguments("hello", 5)]
                    [Arguments("world", 5)]
                    public async Task StringLengthTest(string input, int expectedLength)
                    {
                        await Assert.That(input.Length).IsEqualTo(expectedLength);
                        await Assert.That(input).IsNotNull();
                    }

                    [Test]
                    [Property("Category", "Null")]
                    public async Task NullAndTypeTests()
                    {
                        object obj = "test";
                        object nullObj = null;

                        await Assert.That(obj).IsNotNull();
                        await Assert.That(nullObj).IsNull();
                        await Assert.That(obj).IsAssignableTo(typeof(string));
                    }

                    [Test]
                    public async Task CollectionTests()
                    {
                        var list = new List<int> { 1, 2, 3 };
                        var empty = new List<int>();

                        await Assert.That(list).Contains(2);
                        await Assert.That(list).DoesNotContain(4);
                    }

                    [Test]
                    public async Task StringTests()
                    {
                        var str = "Hello World";

                        await Assert.That(str).StartsWith("Hello");
                        await Assert.That(str).EndsWith("World");
                        await Assert.That(str).Contains("lo Wo");
                    }

                    [Test]
                    [Skip("This test is temporarily disabled")]
                    public async Task IgnoredTest()
                    {
                        await Assert.Fail("Should not run");
                    }
                }
                [Property("Category", "Secondary")]
                public class SecondaryTests
                {
                    [Test]
                    public async Task AnotherTest()
                    {
                        await Assert.That(1).IsEqualTo(1);
                    }

                    [Test]
                    [Arguments(true)]
                    [Arguments(false)]
                    public async Task BooleanTest(bool value)
                    {
                        await Assert.That(value).IsEqualTo(value);
                    }
                }
                """,
            ConfigureMSTestTest
        );
    }

    [Test]
    public async Task MSTest_Method_With_Ref_Parameter_Not_Converted_To_Async()
    {
        // Test that methods with ref parameters use .Wait() instead of await
        await CodeFixer.VerifyCodeFixAsync(
            """
                using Microsoft.VisualStudio.TestTools.UnitTesting;

                {|#0:[TestClass]|}
                public class MyClass
                {
                    [TestMethod]
                    public void MyTest()
                    {
                        bool realized = false;
                        HandleRealized(this, ref realized);
                    }

                    private static void HandleRealized(object sender, ref bool realized)
                    {
                        Assert.IsNotNull(sender);
                        realized = true;
                    }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
            """
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
            ConfigureMSTestTest
        );
    }

    [Test]
    public async Task MSTest_Method_With_Out_Parameter_Not_Converted_To_Async()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                using Microsoft.VisualStudio.TestTools.UnitTesting;

                {|#0:[TestClass]|}
                public class MyClass
                {
                    [TestMethod]
                    public void MyTest()
                    {
                        TryGetValue("key", out int value);
                        Assert.AreEqual(42, value);
                    }

                    private static void TryGetValue(string key, out int value)
                    {
                        Assert.IsNotNull(key);
                        value = 42;
                    }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
            """
                using System.Threading.Tasks;

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
            ConfigureMSTestTest
        );
    }

    [Test]
    public async Task MSTest_InterfaceImplementation_NotConvertedToAsync()
    {
        // Methods that implement interface members should NOT be converted to async
        await CodeFixer.VerifyCodeFixAsync(
            """
                using Microsoft.VisualStudio.TestTools.UnitTesting;
                using System.Threading.Tasks;

                public interface ITestRunner
                {
                    void Run();
                }

                {|#0:[TestClass]|}
                public class MyClass : ITestRunner
                {
                    [TestMethod]
                    public void TestMethod()
                    {
                        Assert.IsTrue(true);
                    }

                    public void Run()
                    {
                        // This implements ITestRunner.Run() and should stay void
                        var x = 1;
                    }
                }
                """,
            Verifier.Diagnostic(Rules.MSTestMigration).WithLocation(0),
            """
                using System.Threading.Tasks;

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
            ConfigureMSTestTest
        );
    }

    // NOTE: MSTest lifecycle visibility changes and DoNotParallelize conversion are not implemented
    // These features exist in NUnit migration but not MSTest migration

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
