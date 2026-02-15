using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using CodeFixer = TUnit.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<TUnit.Analyzers.XUnitMigrationAnalyzer, TUnit.Analyzers.CodeFixers.XUnitMigrationCodeFixProvider>;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.XUnitMigrationAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class XUnitMigrationAnalyzerTests
{
    [Arguments("Fact")]
    [Arguments("Theory")]
    [Arguments("Xunit.Fact")]
    [Arguments("Xunit.Theory")]
    [Test]
    public async Task Test_Attribute_Flagged(string attributeName)
    {
        await Verifier
            .VerifyAnalyzerAsync(
                $$"""
                {|#0:public class MyClass
                {
                    [{{attributeName}}]
                    public void MyTest()
                    {
                    }
                }|}
                """,
                ConfigureXUnitTest,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0)
            );
    }

    [Arguments("Fact", "Test")]
    [Arguments("Theory", "Test")]
    [Arguments("InlineData", "Arguments")]
    [Arguments("Trait(\"Key\", \"Value\")", "Property(\"Key\", \"Value\")")]
    [Arguments("MemberData(\"SomeMethod\")", "MethodDataSource(\"SomeMethod\")")]
    [Arguments("ClassData(typeof(MyClass))", "MethodDataSource(typeof(MyClass), \"GetEnumerator\")")]
    [Arguments("Xunit.Fact", "Test")]
    [Arguments("Xunit.Theory", "Test")]
    [Arguments("Xunit.InlineData", "Arguments")]
    [Arguments("Xunit.Trait(\"Key\", \"Value\")", "Property(\"Key\", \"Value\")")]
    [Arguments("Xunit.MemberData(\"SomeMethod\")", "MethodDataSource(\"SomeMethod\")")]
    [Arguments("Xunit.ClassData(typeof(MyClass))", "MethodDataSource(typeof(MyClass), \"GetEnumerator\")")]
    [Test]
    public async Task Test_Attributes_Can_Be_Fixed(string attribute, string expected)
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                $$"""
                {|#0:using Xunit;

                public class MyClass
                {
                    [{{attribute}}]
                    public void MyTest()
                    {
                    }
                }|}
                """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                $$"""

                public class MyClass
                {
                    [{{expected}}]
                    public void MyTest()
                    {
                    }
                }
                """,
                ConfigureXUnitTest
            );
    }

    [Test]
    [Arguments("Fact")]
    [Arguments("Theory")]
    [Arguments("Xunit.Fact")]
    [Arguments("Xunit.Theory")]
    public async Task Skipped_Test_Attributes_Can_Be_Fixed(string attribute)
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                $$"""
                  {|#0:using Xunit;

                  public class MyClass
                  {
                      [{{attribute}}(Skip = "Reason")]
                      public void MyTest()
                      {
                      }
                  }|}
                  """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                $$"""

                  public class MyClass
                  {
                      [Test]
                      [Skip("Reason")]
                      public void MyTest()
                      {
                      }
                  }
                  """,
                ConfigureXUnitTest
            );
    }

    [Test]
    public async Task Collection_Attributes_Can_Be_Fixed()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                {|#0:using Xunit;

                public class MyType;

                [Collection("MyCollection")]
                public class MyClass
                {
                    [Test]
                    public void MyTest()
                    {
                    }
                }

                [CollectionDefinition("MyCollection")]
                public class MyCollection : ICollectionFixture<MyType>
                {
                }|}
                """,
                [
                    Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                    ],
                    """

                    public class MyType;

                    [ClassDataSource<MyType>(Shared = SharedType.Keyed, Key = "MyCollection")]
                    public class MyClass
                    {
                        [Test]
                        public void MyTest()
                        {
                        }
                    }

                    [System.Obsolete]
                    public class MyCollection
                    {
                    }
                    """,
                    ConfigureXUnitTest
            );
    }

    [Test]
    public async Task Collection_Disable_Parallelism_Attributes_Can_Be_Fixed()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                {|#0:using Xunit;

                public class MyType;

                [Collection("MyCollection")]
                public class MyClass
                {
                    [Test]
                    public void MyTest()
                    {
                    }
                }

                [CollectionDefinition("MyCollection", DisableParallelization = true)]
                public class MyCollection
                {
                }|}
                """,
                [
                    Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                ],
                """

                public class MyType;

                [NotInParallel]
                public class MyClass
                {
                    [Test]
                    public void MyTest()
                    {
                    }
                }

                [System.Obsolete]
                public class MyCollection
                {
                }
                """,
                ConfigureXUnitTest
            );
    }

    [Test]
    public async Task Combined_Collection_Fixture_And_Disable_Parallelism_Attributes_Can_Be_Fixed()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                {|#0:using Xunit;

                public class MyType;

                [Collection("MyCollection")]
                public class MyClass
                {
                    [Test]
                    public void MyTest()
                    {
                    }
                }

                [CollectionDefinition("MyCollection", DisableParallelization = true)]
                public class MyCollection : ICollectionFixture<MyType>
                {
                }|}
                """,
                [
                    Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                ],
                """

                public class MyType;

                [ClassDataSource<MyType>(Shared = SharedType.Keyed, Key = "MyCollection")]
                [NotInParallel]
                public class MyClass
                {
                    [Test]
                    public void MyTest()
                    {
                    }
                }

                [System.Obsolete]
                public class MyCollection
                {
                }
                """,
                ConfigureXUnitTest
            );
    }

    [Test]
    [Arguments("AssemblyFixture(typeof(Exception))", "ClassDataSource<Exception>(Shared = SharedType.PerAssembly)")]
    [Skip("Assembly attribute migration is not yet implemented - requires analyzer support for assembly-level attributes")]
    public async Task Assembly_Attributes_Can_Be_Fixed(string attribute, string expected)
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                $$"""
                  {|#0:using System;
                  using TUnit.Core;

                  [assembly: {|#0:{{attribute}}|}]
                  namespace MyNamespace;

                  public class MyClass
                  {
                      [Test]
                      public void MyTest()
                      {
                      }
                  }|}
                  """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                $$"""
                  using System;
                  using TUnit.Core;

                  [assembly: {{expected}}]
                  namespace MyNamespace;

                  public class MyClass
                  {
                      [Test]
                      public void MyTest()
                      {
                      }
                  }
                  """,
                ConfigureXUnitTest
            );
    }

    [Test]
    public async Task ClassFixture_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                {|#0:public class MyType;

                public class MyClass : IClassFixture<MyType>
                {
                    [Fact]
                    public void MyTest()
                    {
                    }
                }|}
                """,
                ConfigureXUnitTest,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0)
            );
    }

    [Test]
    public async Task ClassFixture_Can_Be_Fixed()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                {|#0:using Xunit;

                public class MyType;

                public class MyClass(MyType myType) : IClassFixture<MyType>
                {
                    [Fact]
                    public void MyTest()
                    {
                    }
                }|}
                """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                """

                public class MyType;

                [ClassDataSource<MyType>(Shared = SharedType.PerClass)]
                public class MyClass(MyType myType)
                {
                    [Test]
                    public void MyTest()
                    {
                    }
                }
                """,
                ConfigureXUnitTest
            );
    }

    [Test]
    public async Task Xunit_Directive_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                {|#0:using Xunit;
                using Xunit;

                public class MyClass
                {
                    [Fact]
                    public void MyTest()
                    {
                    }
                }|}
                """,
                ConfigureXUnitTest,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0)
            );
    }

    [Test]
    public async Task Xunit_Directive_Can_Be_Removed()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                {|#0:using Xunit;
                using Xunit;

                public class MyClass
                {
                    [Fact]
                    public void MyTest()
                    {
                    }
                }|}
                """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                """

                public class MyClass
                {
                    [Test]
                    public void MyTest()
                    {
                    }
                }
                """,
                ConfigureXUnitTest
            );
    }

    [Test]
    public async Task Test_Initialize_Can_Be_Converted()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                {|#0:using Xunit;

                public class MyClass : IAsyncLifetime
                {
                    public ValueTask InitializeAsync()
                    {
                        return default;
                    }

                    public ValueTask DisposeAsync()
                    {
                        return default;
                    }

                    [Fact]
                    public void MyTest()
                    {
                    }
                }|}
                """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                """

                public class MyClass
                {
                    [Before(Test)]
                    public Task InitializeAsync()
                    {
                        return default;
                    }

                    [After(Test)]
                    public Task DisposeAsync()
                    {
                        return default;
                    }

                    [Test]
                    public void MyTest()
                    {
                    }
                }
                """,
                ConfigureXUnitTest
            );
    }

    [Test]
    public async Task NonTest_Initialize_Can_Be_Converted()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                {|#0:using Xunit;

                public class MyClass : IAsyncLifetime
                {
                    public ValueTask InitializeAsync()
                    {
                        return default;
                    }

                    public ValueTask DisposeAsync()
                    {
                        return default;
                    }
                }|}
                """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                """

                public class MyClass : IAsyncInitializer, IAsyncDisposable
                {
                    public Task InitializeAsync()
                    {
                        return default;
                    }

                    public ValueTask DisposeAsync()
                    {
                        return default;
                    }
                }
                """,
                ConfigureXUnitTest
            );
    }

    [Test]
    public async Task TheoryData_Is_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                {|#0:using System;

                public class MyClass
                {
                    public static readonly TheoryData<TimeSpan> Times = new()
                    {
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromHours(1),
                        TimeSpan.FromMilliseconds(10)
                    };
                }|}
                """,
                ConfigureXUnitTest,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0)
            );
    }

    [Test]
    public async Task TheoryData_Can_Be_Converted()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                {|#0:using Xunit;

                public class MyClass
                {
                    public static readonly TheoryData<TimeSpan> Times = new()
                    {
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromHours(1),
                        TimeSpan.FromMilliseconds(10)
                    };
                }|}
                """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                """

                public class MyClass
                {
                    public static readonly IEnumerable<TimeSpan> Times = new TimeSpan[]
                    {
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromHours(1),
                        TimeSpan.FromMilliseconds(10)
                    };
                }
                """,
                ConfigureXUnitTest
            );
    }

    [Test]
    public async Task ITestOutputHelper_Is_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                {|#0:using System;

                public class UnitTest1(ITestOutputHelper testOutputHelper)
                {
                    private ITestOutputHelper _testOutputHelper = testOutputHelper;
                    public ITestOutputHelper TestOutputHelper { get; } = testOutputHelper;

                    [Fact]
                    public void Test1()
                    {
                        _testOutputHelper.WriteLine("Foo");
                        TestOutputHelper.WriteLine("Bar");
                    }
                }|}
                """,
                ConfigureXUnitTest,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0)
            );
    }

    [Test]
    public async Task ITestOutputHelper_Can_Be_Converted()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                {|#0:using Xunit;

                public class UnitTest1(ITestOutputHelper testOutputHelper)
                {
                    private ITestOutputHelper _testOutputHelper = testOutputHelper;
                    public ITestOutputHelper TestOutputHelper { get; } = testOutputHelper;

                    [Fact]
                    public void Test1()
                    {
                        _testOutputHelper.WriteLine("Foo");
                        TestOutputHelper.WriteLine("Bar");
                    }
                }|}
                """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                """

                public class UnitTest1()
                {
                    [Test]
                    public void Test1()
                    {
                        Console.WriteLine("Foo");
                        Console.WriteLine("Bar");
                    }
                }
                """,
                ConfigureXUnitTest
            );
    }

    [Test]
    public async Task Assert_Equal_Can_Be_Converted()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                {|#0:using Xunit;

                public class MyClass
                {
                    [Fact]
                    public void MyTest()
                    {
                        Assert.Equal(5, 2 + 3);
                    }
                }|}
                """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task MyTest()
                    {
                        await Assert.That(2 + 3).IsEqualTo(5);
                    }
                }
                """,
                ConfigureXUnitTest
            );
    }

    [Test]
    public async Task Assert_Matches_Can_Be_Converted()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                {|#0:using Xunit;

                public class MyClass
                {
                    [Fact]
                    public void MyTest()
                    {
                        Assert.Matches(@"\d+", "abc123");
                    }
                }|}
                """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task MyTest()
                    {
                        await Assert.That("abc123").Matches(@"\d+");
                    }
                }
                """,
                ConfigureXUnitTest
            );
    }

    [Test]
    public async Task Assert_DoesNotMatch_Can_Be_Converted()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                {|#0:using Xunit;

                public class MyClass
                {
                    [Fact]
                    public void MyTest()
                    {
                        Assert.DoesNotMatch(@"^\d+$", "abc123");
                    }
                }|}
                """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task MyTest()
                    {
                        await Assert.That("abc123").DoesNotMatch(@"^\d+$");
                    }
                }
                """,
                ConfigureXUnitTest
            );
    }

    [Test]
    public async Task Assert_Collection_Adds_Todo_Comment()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                {|#0:using System;
                using Xunit;

                public class MyClass
                {
                    [Fact]
                    public void MyTest()
                    {
                        var items = new[] { 1, 2, 3 };
                        Assert.Collection(items,
                            x => Assert.Equal(1, x),
                            x => Assert.Equal(2, x),
                            x => Assert.Equal(3, x));
                    }
                }|}
                """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                """
                using System;
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task MyTest()
                    {
                        var items = new[] { 1, 2, 3 };
                        // TODO: TUnit migration - Assert.Collection had element inspectors. Manually add assertions for each element.
                        await Assert.That(items).HasCount(3);
                    }
                }
                """,
                ConfigureXUnitTest
            );
    }

    [Test]
    public async Task Assert_ProperSubset_Adds_Todo_Comment()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                {|#0:using System;
                using System.Collections.Generic;
                using Xunit;

                public class MyClass
                {
                    [Fact]
                    public void MyTest()
                    {
                        var subset = new HashSet<int> { 1, 2 };
                        var superset = new HashSet<int> { 1, 2, 3 };
                        Assert.ProperSubset(superset, subset);
                    }
                }|}
                """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task MyTest()
                    {
                        var subset = new HashSet<int> { 1, 2 };
                        var superset = new HashSet<int> { 1, 2, 3 };
                        // TODO: TUnit migration - ProperSubset requires strict subset (not equal). Add additional assertion if needed.
                        await Assert.That(superset).IsSubsetOf(subset);
                    }
                }
                """,
                ConfigureXUnitTest
            );
    }

    [Test]
    public async Task Assert_ProperSuperset_Adds_Todo_Comment()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                {|#0:using System;
                using System.Collections.Generic;
                using Xunit;

                public class MyClass
                {
                    [Fact]
                    public void MyTest()
                    {
                        var subset = new HashSet<int> { 1, 2 };
                        var superset = new HashSet<int> { 1, 2, 3 };
                        Assert.ProperSuperset(subset, superset);
                    }
                }|}
                """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task MyTest()
                    {
                        var subset = new HashSet<int> { 1, 2 };
                        var superset = new HashSet<int> { 1, 2, 3 };
                        // TODO: TUnit migration - ProperSuperset requires strict superset (not equal). Add additional assertion if needed.
                        await Assert.That(subset).IsSupersetOf(superset);
                    }
                }
                """,
                ConfigureXUnitTest
            );
    }

    [Test]
    public async Task Assert_Same_Can_Be_Converted()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                {|#0:using Xunit;

                public class MyClass
                {
                    [Fact]
                    public void MyTest()
                    {
                        var expected = new object();
                        var actual = expected;
                        Assert.Same(expected, actual);
                    }
                }|}
                """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task MyTest()
                    {
                        var expected = new object();
                        var actual = expected;
                        await Assert.That(actual).IsSameReferenceAs(expected);
                    }
                }
                """,
                ConfigureXUnitTest
            );
    }

    [Test]
    public async Task Assert_NotSame_Can_Be_Converted()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                {|#0:using Xunit;

                public class MyClass
                {
                    [Fact]
                    public void MyTest()
                    {
                        var obj1 = new object();
                        var obj2 = new object();
                        Assert.NotSame(obj1, obj2);
                    }
                }|}
                """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task MyTest()
                    {
                        var obj1 = new object();
                        var obj2 = new object();
                        await Assert.That(obj2).IsNotSameReferenceAs(obj1);
                    }
                }
                """,
                ConfigureXUnitTest
            );
    }

    [Test]
    public async Task Assert_All_Can_Be_Converted()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                {|#0:using System;
                using Xunit;

                public class MyClass
                {
                    [Fact]
                    public void MyTest()
                    {
                        var items = new[] { 1, 2, 3 };
                        Assert.All(items, item => Assert.True(item > 0));
                    }
                }|}
                """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                """
                using System;
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task MyTest()
                    {
                        var items = new[] { 1, 2, 3 };
                        await Assert.That(items).All(item => item > 0);
                    }
                }
                """,
                ConfigureXUnitTest
            );
    }

    [Test]
    public async Task Record_Exception_Can_Be_Converted()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                {|#0:using System;
                using Xunit;

                public class MyClass
                {
                    [Fact]
                    public void MyTest()
                    {
                        var ex = Record.Exception(() => throw new InvalidOperationException("Test"));
                        Assert.NotNull(ex);
                        Assert.IsType<InvalidOperationException>(ex);
                    }
                }|}
                """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                """
                using System;
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task MyTest()
                    {
                        Exception? ex = null;
                        try
                        {
                            throw new InvalidOperationException("Test");
                        }
                        catch (Exception e)
                        {
                            ex = e;
                        }
                        await Assert.That(ex).IsNotNull();
                        await Assert.That(ex).IsTypeOf<InvalidOperationException>();
                    }
                }
                """,
                ConfigureXUnitTest
            );
    }

    [Test]
    public async Task Assert_Throws_With_Property_Access_Can_Be_Converted()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                {|#0:using System;
                using Xunit;

                public class MyClass
                {
                    [Fact]
                    public void MyTest()
                    {
                        var ex = Assert.Throws<ArgumentException>(() => ThrowException());
                        Assert.Equal("param", ex.ParamName);
                    }

                    private void ThrowException() => throw new ArgumentException("error", "param");
                }|}
                """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                """
                using System;
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task MyTest()
                    {
                        var ex = Assert.Throws<ArgumentException>(() => ThrowException());
                        await Assert.That(ex.ParamName).IsEqualTo("param");
                    }

                    private void ThrowException() => throw new ArgumentException("error", "param");
                }
                """,
                ConfigureXUnitTest
            );
    }

    [Test]
    public async Task Assert_Throws_With_Message_Contains_Can_Be_Converted()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                {|#0:using System;
                using Xunit;

                public class MyClass
                {
                    [Fact]
                    public void MyTest()
                    {
                        var ex = Assert.Throws<InvalidOperationException>(() => ThrowException());
                        Assert.Contains("error occurred", ex.Message);
                    }

                    private void ThrowException() => throw new InvalidOperationException("An error occurred");
                }|}
                """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                """
                using System;
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task MyTest()
                    {
                        var ex = Assert.Throws<InvalidOperationException>(() => ThrowException());
                        await Assert.That(ex.Message).Contains("error occurred");
                    }

                    private void ThrowException() => throw new InvalidOperationException("An error occurred");
                }
                """,
                ConfigureXUnitTest
            );
    }

    [Test]
    public async Task Record_Exception_With_Method_Call_Can_Be_Converted()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                {|#0:using System;
                using Xunit;

                public class MyClass
                {
                    [Fact]
                    public void MyTest()
                    {
                        var ex = Record.Exception(() => DoSomething());
                        Assert.Null(ex);
                    }

                    private void DoSomething() { }
                }|}
                """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                """
                using System;
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task MyTest()
                    {
                        Exception? ex = null;
                        try
                        {
                            DoSomething();
                        }
                        catch (Exception e)
                        {
                            ex = e;
                        }
                        await Assert.That(ex).IsNull();
                    }

                    private void DoSomething() { }
                }
                """,
                ConfigureXUnitTest
            );
    }

    [Test]
    public async Task XUnit_KitchenSink_Comprehensive_Migration()
    {
        // This test combines MANY xUnit patterns together to ensure the code fixer
        // can handle complex real-world scenarios in a single pass.
        // Note: IClassFixture + IAsyncLifetime combined have complex interactions -
        // this test focuses on patterns that work reliably together.
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                {|#0:using Xunit;
                using System;
                using System.Collections.Generic;

                public class KitchenSinkTests
                {
                    [Fact]
                    public void BasicTest()
                    {
                        var value = 42;
                        Assert.NotNull(value);
                        Assert.Equal(42, value);
                    }

                    [Theory]
                    [InlineData(1, 2, 3)]
                    [InlineData(10, 20, 30)]
                    [InlineData(-1, 1, 0)]
                    public void ParameterizedTest(int a, int b, int expected)
                    {
                        var result = a + b;
                        Assert.Equal(expected, result);
                    }

                    [Theory]
                    [MemberData(nameof(GetTestData))]
                    public void DataSourceTest(string input, int expectedLength)
                    {
                        Assert.Equal(expectedLength, input.Length);
                        Assert.NotNull(input);
                    }

                    public static IEnumerable<object[]> GetTestData()
                    {
                        yield return new object[] { "hello", 5 };
                        yield return new object[] { "world", 5 };
                    }

                    [Fact]
                    public void CollectionAssertTest()
                    {
                        var list = new List<int> { 1, 2, 3 };
                        Assert.Contains(2, list);
                        Assert.NotEmpty(list);
                    }

                    [Fact]
                    public void StringAssertTest()
                    {
                        var text = "Hello World";
                        Assert.Contains("World", text);
                        Assert.StartsWith("Hello", text);
                        Assert.EndsWith("World", text);
                    }

                    [Fact]
                    public void ExceptionTest()
                    {
                        Assert.Throws<ArgumentException>(() => throw new ArgumentException("test"));
                    }

                    [Fact]
                    public async Task AsyncExceptionTest()
                    {
                        await Assert.ThrowsAsync<ArgumentException>(async () =>
                        {
                            await Task.CompletedTask;
                            throw new ArgumentException("test");
                        });
                    }

                    [Fact]
                    public void ComparisonAssertions()
                    {
                        var value = 42;
                        Assert.True(value > 0);
                        Assert.False(value < 0);
                        Assert.NotEqual(0, value);
                        Assert.InRange(value, 0, 100);
                    }

                    [Fact]
                    public void NullAssertions()
                    {
                        string? nullValue = null;
                        var notNullValue = "test";
                        Assert.Null(nullValue);
                        Assert.NotNull(notNullValue);
                    }

                    [Fact]
                    public void TypeAssertions()
                    {
                        object obj = "test string";
                        Assert.IsType<string>(obj);
                        Assert.IsAssignableFrom<object>(obj);
                    }
                }|}
                """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;

                public class KitchenSinkTests
                {
                    [Test]
                    public async Task BasicTest()
                    {
                        var value = 42;
                        await Assert.That(value).IsNotNull();
                        await Assert.That(value).IsEqualTo(42);
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
                        await Assert.That(list).IsNotEmpty();
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
                    public void ExceptionTest()
                    {
                        Assert.Throws<ArgumentException>(() => throw new ArgumentException("test"));
                    }

                    [Test]
                    public async Task AsyncExceptionTest()
                    {
                        await Assert.ThrowsAsync<ArgumentException>(async () =>
                        {
                            await Task.CompletedTask;
                            throw new ArgumentException("test");
                        });
                    }

                    [Test]
                    public async Task ComparisonAssertions()
                    {
                        var value = 42;
                        await Assert.That(value > 0).IsTrue();
                        await Assert.That(value < 0).IsFalse();
                        await Assert.That(value).IsNotEqualTo(0);
                        await Assert.That(value).IsInRange(0,100);
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
                        await Assert.That(obj).IsTypeOf<string>();
                        await Assert.That(obj).IsAssignableTo<object>();
                    }
                }
                """,
                ConfigureXUnitTest
            );
    }

    [Test]
    public async Task XUnit_Assert_Empty_List_Converted()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                {|#0:using Xunit;
                using System.Collections.Generic;

                public class MyClass
                {
                    [Fact]
                    public void MyTest()
                    {
                        var items = new List<string>();
                        Assert.Empty(items);
                    }
                }|}
                """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                """
                using System.Collections.Generic;
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task MyTest()
                    {
                        var items = new List<string>();
                        await Assert.That(items).IsEmpty();
                    }
                }
                """,
                ConfigureXUnitTest
            );
    }

    [Test]
    public async Task XUnit_Assert_NotEmpty_List_Converted()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                {|#0:using Xunit;
                using System.Collections.Generic;

                public class MyClass
                {
                    [Fact]
                    public void MyTest()
                    {
                        var items = new List<string> { "item" };
                        Assert.NotEmpty(items);
                    }
                }|}
                """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                """
                using System.Collections.Generic;
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task MyTest()
                    {
                        var items = new List<string> { "item" };
                        await Assert.That(items).IsNotEmpty();
                    }
                }
                """,
                ConfigureXUnitTest
            );
    }

    [Test]
    public async Task XUnit_Assert_Empty_Array_Converted()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                {|#0:using Xunit;

                public class MyClass
                {
                    [Fact]
                    public void MyTest()
                    {
                        var items = new string[0];
                        Assert.Empty(items);
                    }
                }|}
                """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task MyTest()
                    {
                        var items = new string[0];
                        await Assert.That(items).IsEmpty();
                    }
                }
                """,
                ConfigureXUnitTest
            );
    }

    [Test]
    public async Task XUnit_Using_Directive_Removed()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                {|#0:using Xunit;
                using Xunit.Abstractions;

                public class MyClass
                {
                    [Fact]
                    public void MyTest()
                    {
                        Assert.True(true);
                    }
                }|}
                """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task MyTest()
                    {
                        await Assert.That(true).IsTrue();
                    }
                }
                """,
                ConfigureXUnitTest
            );
    }

    [Test]
    public async Task XUnit_Comprehensive_Kitchen_Sink_Migration()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                {|#0:using Xunit;
                using System;
                using System.Collections.Generic;

                public class ComprehensiveTests
                {
                    private List<string> _log;
                    private int _counter;

                    public ComprehensiveTests()
                    {
                        // Constructor acts as setup
                        _log = new List<string>();
                        _counter = 0;
                    }

                    [Fact]
                    public void SimpleTest()
                    {
                        Assert.True(true);
                        Assert.False(false);
                    }

                    [Fact]
                    public void MathTest()
                    {
                        var result = 2 + 2;
                        Assert.Equal(4, result);
                        Assert.NotEqual(5, result);
                    }

                    [Theory]
                    [InlineData(1, 2, 3)]
                    [InlineData(5, 5, 10)]
                    [InlineData(-1, 1, 0)]
                    public void AdditionTest(int a, int b, int expected)
                    {
                        var result = a + b;
                        Assert.Equal(expected, result);
                    }

                    [Theory]
                    [InlineData("hello", 5)]
                    [InlineData("world", 5)]
                    public void StringLengthTest(string input, int expectedLength)
                    {
                        Assert.Equal(expectedLength, input.Length);
                        Assert.NotNull(input);
                    }

                    [Fact]
                    public void NullAndTypeTests()
                    {
                        object obj = "test";
                        object nullObj = null;

                        Assert.NotNull(obj);
                        Assert.Null(nullObj);
                        Assert.IsType<string>(obj);
                        Assert.IsAssignableFrom<object>(obj);
                    }

                    [Fact]
                    public void CollectionTests()
                    {
                        var list = new List<int> { 1, 2, 3 };
                        var empty = new List<int>();

                        Assert.NotEmpty(list);
                        Assert.Empty(empty);
                        Assert.Contains(2, list);
                        Assert.DoesNotContain(4, list);
                    }

                    [Fact]
                    public void StringTests()
                    {
                        var str = "Hello World";

                        Assert.StartsWith("Hello", str);
                        Assert.EndsWith("World", str);
                        Assert.Contains("lo Wo", str);
                    }

                    [Fact(Skip = "This test is temporarily disabled")]
                    public void SkippedTest()
                    {
                        Assert.True(false, "Should not run");
                    }
                }

                public class SecondaryTests
                {
                    [Fact]
                    public void AnotherTest()
                    {
                        Assert.Equal(1, 1);
                    }

                    [Theory]
                    [InlineData(true)]
                    [InlineData(false)]
                    public void BooleanTest(bool value)
                    {
                        Assert.Equal(value, value);
                    }
                }|}
                """,
            Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
            """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;

                public class ComprehensiveTests
                {
                    private List<string> _log;
                    private int _counter;

                    public ComprehensiveTests()
                    {
                        // Constructor acts as setup
                        _log = new List<string>();
                        _counter = 0;
                    }

                    [Test]
                    public async Task SimpleTest()
                    {
                        await Assert.That(true).IsTrue();
                        await Assert.That(false).IsFalse();
                    }

                    [Test]
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
                    public async Task NullAndTypeTests()
                    {
                        object obj = "test";
                        object nullObj = null;

                        await Assert.That(obj).IsNotNull();
                        await Assert.That(nullObj).IsNull();
                        await Assert.That(obj).IsTypeOf<string>();
                        await Assert.That(obj).IsAssignableTo<object>();
                    }

                    [Test]
                    public async Task CollectionTests()
                    {
                        var list = new List<int> { 1, 2, 3 };
                        var empty = new List<int>();

                        await Assert.That(list).IsNotEmpty();
                        await Assert.That(empty).IsEmpty();
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
                    public async Task SkippedTest()
                    {
                        await Assert.That(false).IsTrue().Because("Should not run");
                    }
                }

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
            ConfigureXUnitTest
        );
    }

    [Test]
    public async Task XUnit_Method_With_Ref_Parameter_Not_Converted_To_Async()
    {
        // Test that methods with ref parameters use .Wait() instead of await
        await CodeFixer.VerifyCodeFixAsync(
            """
                {|#0:using Xunit;

                public class MyClass
                {
                    [Fact]
                    public void MyTest()
                    {
                        bool realized = false;
                        HandleRealized(this, ref realized);
                    }

                    private static void HandleRealized(object sender, ref bool realized)
                    {
                        Assert.NotNull(sender);
                        realized = true;
                    }
                }|}
                """,
            Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
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
            ConfigureXUnitTest
        );
    }

    [Test]
    public async Task XUnit_Method_With_Out_Parameter_Not_Converted_To_Async()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                {|#0:using Xunit;

                public class MyClass
                {
                    [Fact]
                    public void MyTest()
                    {
                        TryGetValue("key", out int value);
                        Assert.Equal(42, value);
                    }

                    private static void TryGetValue(string key, out int value)
                    {
                        Assert.NotNull(key);
                        value = 42;
                    }
                }|}
                """,
            Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
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
            ConfigureXUnitTest
        );
    }

    [Test]
    public async Task XUnit_InterfaceImplementation_NotConvertedToAsync()
    {
        // Methods that implement interface members should NOT be converted to async
        await CodeFixer.VerifyCodeFixAsync(
            """
                {|#0:using Xunit;
                using System.Threading.Tasks;

                public interface ITestRunner
                {
                    void Run();
                }

                public class MyClass : ITestRunner
                {
                    [Fact]
                    public void TestMethod()
                    {
                        Assert.True(true);
                    }

                    public void Run()
                    {
                        // This implements ITestRunner.Run() and should stay void
                        var x = 1;
                    }
                }|}
                """,
            Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
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
            ConfigureXUnitTest
        );
    }

    [Test]
    public async Task XUnit_Nested_Class_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                {|#0:using Xunit;

                public class OuterClass
                {
                    public class InnerTests
                    {
                        [Fact]
                        public void InnerTest()
                        {
                            Assert.True(true);
                        }
                    }

                    [Fact]
                    public void OuterTest()
                    {
                        Assert.False(false);
                    }
                }|}
                """,
            Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
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
            ConfigureXUnitTest
        );
    }

    [Test]
    public async Task XUnit_Multiple_Classes_In_File_All_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                {|#0:using Xunit;

                public class FirstTestClass
                {
                    [Fact]
                    public void FirstTest()
                    {
                        Assert.True(true);
                    }
                }

                public class SecondTestClass
                {
                    [Fact]
                    public void SecondTest()
                    {
                        Assert.False(false);
                    }
                }

                public class ThirdTestClass
                {
                    [Fact]
                    public void ThirdTest()
                    {
                        Assert.Equal(1, 1);
                    }
                }|}
                """,
            Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
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
                    [Test]
                    public async Task ThirdTest()
                    {
                        await Assert.That(1).IsEqualTo(1);
                    }
                }
                """,
            ConfigureXUnitTest
        );
    }

    [Test]
    public async Task XUnit_Record_Exception_DoesNotThrow_Pattern()
    {
        // Record.Exception returning null is equivalent to DoesNotThrow
        // The migration converts Record.Exception to a try-catch pattern
        await CodeFixer.VerifyCodeFixAsync(
            """
                {|#0:using Xunit;

                public class MyClass
                {
                    [Fact]
                    public void TestMethod()
                    {
                        int x = 1;
                        int y = 2;
                        var ex = Record.Exception(() => x += y);
                        Assert.Null(ex);
                    }
                }|}
                """,
            Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
            """
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task TestMethod()
                    {
                        int x = 1;
                        int y = 2;
                        Exception? ex = null;
                        try
                        {
                            x += y;
                        }
                        catch (Exception e)
                        {
                            ex = e;
                        }
                        await Assert.That(ex).IsNull();
                    }
                }
                """,
            ConfigureXUnitTest
        );
    }

    [Test]
    public async Task XUnit_Generic_Test_Class_Converted()
    {
        await CodeFixer.VerifyCodeFixAsync(
            """
                {|#0:using Xunit;

                public class GenericTestClass<T>
                {
                    [Fact]
                    public void GenericTest()
                    {
                        var instance = default(T);
                        Assert.Equal(default(T), instance);
                    }
                }|}
                """,
            Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
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
            ConfigureXUnitTest
        );
    }

    [Test]
    public async Task XUnit_Assert_Contains_Predicate_Overload_Converted()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                {|#0:using Xunit;

                public class MyClass
                {
                    [Fact]
                    public void MyTest()
                    {
                        var numbers = new[] { 22, 75, 19 };
                        Assert.Contains(numbers, x => x == 22);
                    }
                }|}
                """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                """
                using System.Threading.Tasks;

                public class MyClass
                {
                    [Test]
                    public async Task MyTest()
                    {
                        var numbers = new[] { 22, 75, 19 };
                        await Assert.That(numbers).Contains(x => x == 22);
                    }
                }
                """,
                ConfigureXUnitTest
            );
    }

    private static void ConfigureXUnitTest(Verifier.Test test)
    {
        var globalUsings = ("GlobalUsings.cs", SourceText.From("global using Xunit;"));

        test.TestState.Sources.Add(globalUsings);
        test.TestState.AdditionalReferences.Add(typeof(Xunit.FactAttribute).Assembly);
        test.TestState.AdditionalReferences.Add(typeof(Xunit.Assert).Assembly);
    }

    private static void ConfigureXUnitTest(CodeFixer.Test test)
    {
        // Add xUnit assemblies to TestState (for input code compilation)
        // Note: Test input code must have explicit "using Xunit;" directives
        test.TestState.AdditionalReferences.Add(typeof(Xunit.FactAttribute).Assembly);
        test.TestState.AdditionalReferences.Add(typeof(Xunit.Assert).Assembly);

        // FixedState: TUnit assemblies only (NO xUnit inheritance)
        // Use Explicit inheritance mode to prevent xUnit references from being inherited
        // This ensures the analyzer's IsFrameworkAvailable check returns false for xUnit
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


