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
    [Arguments("NUnit.Framework.TestCase(1, 2, 3)", "Arguments(1, 2, 3)")]
    [Arguments("NUnit.Framework.SetUp", "Before(HookType.Test)")]
    [Arguments("NUnit.Framework.TearDown", "After(HookType.Test)")]
    [Arguments("NUnit.Framework.OneTimeSetUp", "Before(HookType.Class)")]
    [Arguments("NUnit.Framework.OneTimeTearDown", "After(HookType.Class)")]
    [Arguments("NUnit.Framework.TestCaseSource(\"SomeMethod\")", "MethodDataSource(\"SomeMethod\")")]
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
                        await Assert.That(10).IsGreaterThan(5);
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
    
    private static void ConfigureNUnitTest(Verifier.Test test)
    {
        test.TestState.AdditionalReferences.Add(typeof(NUnit.Framework.TestAttribute).Assembly);
    }

    private static void ConfigureNUnitTest(CodeFixer.Test test)
    {
        test.TestState.AdditionalReferences.Add(typeof(NUnit.Framework.TestAttribute).Assembly);
        test.FixedState.AdditionalReferences.Add(typeof(NUnit.Framework.TestAttribute).Assembly);
    }
}