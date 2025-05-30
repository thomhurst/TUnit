using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.XUnitMigrationAnalyzer>;
using CodeFixer = TUnit.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<TUnit.Analyzers.XUnitMigrationAnalyzer, TUnit.Analyzers.CodeFixers.XUnitMigrationCodeFixProvider>;

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
                {|#0:using TUnit.Core;

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
                using TUnit.Core;

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
                  {|#0:using TUnit.Core;

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
                  using TUnit.Core;

                  public class MyClass
                  {
                      [Test, Skip("Reason")]
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
                {|#0:using TUnit.Core;

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
                    using TUnit.Core;

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
                {|#0:using TUnit.Core;

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
                using TUnit.Core;

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
                {|#0:using TUnit.Core;

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
                using TUnit.Core;

                public class MyType;
                [NotInParallel, ClassDataSource<MyType>(Shared = SharedType.Keyed, Key = "MyCollection")]
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
    [Skip("TODO")]
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
                {|#0:public class MyType;

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
                {|#0:using TUnit.Core;
                using Xunit;

                public class MyClass
                {
                    [Test]
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
                {|#0:using TUnit.Core;
                using Xunit;

                public class MyClass
                {
                    [Test]
                    public void MyTest()
                    {
                    }
                }|}
                """,
                Verifier.Diagnostic(Rules.XunitMigration).WithLocation(0),
                """
                using TUnit.Core;

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
                {|#0:using TUnit.Core;

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
                using TUnit.Core;
                
                public class MyClass
                {
                    [Test]
                    public void MyTest()
                    {
                    }
                
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
                {|#0:using TUnit.Core;

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
                using TUnit.Core;

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
        await CodeFixer
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
                {|#0:using TUnit.Core;

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
                using TUnit.Core;

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
        await CodeFixer
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
                {|#0:using TUnit.Core;

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
                using TUnit.Core;

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

    private static void ConfigureXUnitTest(Verifier.Test test)
    {
        var globalUsings = ("GlobalUsings.cs", SourceText.From("global using Xunit;"));
        
        test.TestState.Sources.Add(globalUsings);
        
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages([
            new PackageIdentity("xunit.v3.extensibility.core", "2.0.0")
        ]);
    }
    
    private static void ConfigureXUnitTest(CodeFixer.Test test)
    {
        var globalUsings = ("GlobalUsings.cs", SourceText.From("global using Xunit;"));
        
        test.TestState.Sources.Add(globalUsings);
        test.FixedState.Sources.Add(globalUsings);

        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages([
            new PackageIdentity("xunit.v3.extensibility.core", "2.0.0")
        ]);
    }
}

