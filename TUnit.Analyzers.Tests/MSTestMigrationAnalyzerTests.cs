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
    
    private static void ConfigureMSTestTest(Verifier.Test test)
    {
        test.TestState.AdditionalReferences.Add(typeof(TestMethodAttribute).Assembly);
    }

    private static void ConfigureMSTestTest(CodeFixer.Test test)
    {
        test.TestState.AdditionalReferences.Add(typeof(TestMethodAttribute).Assembly);
        test.FixedState.AdditionalReferences.Add(typeof(TestMethodAttribute).Assembly);
    }
}