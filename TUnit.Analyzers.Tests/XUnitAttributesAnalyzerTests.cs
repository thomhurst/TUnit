using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.XUnitAttributesAnalyzer>;
using CodeFixer = TUnit.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<TUnit.Analyzers.XUnitAttributesAnalyzer, TUnit.Analyzers.CodeFixers.XUnitAttributesCodeFixProvider>;

namespace TUnit.Analyzers.Tests;

public class XUnitAttributesAnalyzerTests
{
    [Test]
    public async Task Test_Attribute_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using Xunit;

                public class MyClass
                {
                    [{|#0:Fact|}]
                    public void MyTest()
                    {
                    }
                }
                """,
                Verifier.Diagnostic(Rules.XunitAttributes).WithLocation(0)
            );
    }
    
    [TestCase("Fact", "Test")]
    [TestCase("Theory", "Test")]
    [TestCase("InlineData", "Arguments")]
    [TestCase("Trait(\"Key\", \"Value\")", "Property(\"Key\", \"Value\")")]
    [TestCase("MemberData(\"SomeMethod\")", "MethodDataSource(\"SomeMethod\")")]
    [TestCase("ClassData(typeof(MyClass))", "MethodDataSource(typeof(MyClass), \"GetEnumerator\")")]
    public async Task Test_Attributes_Can_Be_Fixed(string attribute, string expected)
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                $$"""
                using TUnit.Core;
                using Xunit;

                public class MyClass
                {
                    [{|#0:{{attribute}}|}]
                    public void MyTest()
                    {
                    }
                }
                """,
                Verifier.Diagnostic(Rules.XunitAttributes).WithLocation(0),
                $$"""
                using TUnit.Core;
                using Xunit;

                public class MyClass
                {
                    [{{expected}}]
                    public void MyTest()
                    {
                    }
                }
                """
            );
    }
    
    [TestCase("Fact")]
    [TestCase("Theory")]
    public async Task Skipped_Test_Attributes_Can_Be_Fixed(string attribute)
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                $$"""
                  using TUnit.Core;
                  using Xunit;

                  public class MyClass
                  {
                      [{|#0:{{attribute}}(Skip = "Reason")|}]
                      public void MyTest()
                      {
                      }
                  }
                  """,
                Verifier.Diagnostic(Rules.XunitAttributes).WithLocation(0),
                $$"""
                  using TUnit.Core;
                  using Xunit;

                  public class MyClass
                  {
                      [Test, Skip("Reason")]
                      public void MyTest()
                      {
                      }
                  }
                  """
            );
    }

    [Test]
    public async Task Collection_Attributes_Can_Be_Fixed()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                using TUnit.Core;
                using Xunit;

                public class MyType;

                [{|#1:Collection("MyCollection")|}]
                public class MyClass
                {
                    [Test]
                    public void MyTest()
                    {
                    }
                }
                
                [{|#0:CollectionDefinition("MyCollection")|}]
                public class MyCollection : ICollectionFixture<MyType>
                {
                }
                """,
                [
                    Verifier.Diagnostic(Rules.XunitAttributes).WithLocation(0),
                    Verifier.Diagnostic(Rules.XunitAttributes).WithLocation(1)
                    ],
                    """
                    using TUnit.Core;
                    using Xunit;

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
                    public class MyCollection : ICollectionFixture<MyType>
                    {
                    }
                    """
            );
    }
    
    [Test]
    public async Task Collection_Disable_Parallelism_Attributes_Can_Be_Fixed()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                using TUnit.Core;
                using Xunit;

                public class MyType;

                [{|#1:Collection("MyCollection")|}]
                public class MyClass
                {
                    [Test]
                    public void MyTest()
                    {
                    }
                }

                [{|#0:CollectionDefinition("MyCollection", DisableParallelization = true)|}]
                public class MyCollection
                {
                }
                """,
                [
                    Verifier.Diagnostic(Rules.XunitAttributes).WithLocation(0),
                    Verifier.Diagnostic(Rules.XunitAttributes).WithLocation(1)
                ],
                """
                using TUnit.Core;
                using Xunit;

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
                """
            );
    }
    
    [Test]
    public async Task Combined_Collection_Fixture_And_Disable_Parallelism_Attributes_Can_Be_Fixed()
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                """
                using TUnit.Core;
                using Xunit;

                public class MyType;

                [{|#1:Collection("MyCollection")|}]
                public class MyClass
                {
                    [Test]
                    public void MyTest()
                    {
                    }
                }

                [{|#0:CollectionDefinition("MyCollection", DisableParallelization = true)|}]
                public class MyCollection : ICollectionFixture<MyType>
                {
                }
                """,
                [
                    Verifier.Diagnostic(Rules.XunitAttributes).WithLocation(0),
                    Verifier.Diagnostic(Rules.XunitAttributes).WithLocation(1)
                ],
                """
                using TUnit.Core;
                using Xunit;

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
                public class MyCollection : ICollectionFixture<MyType>
                {
                }
                """
            );
    }

    [TestCase("AssemblyFixture(typeof(Exception))", "ClassDataSource<Exception>(Shared = SharedType.PerAssembly)")]
    [Ignore("TODO")]
    public async Task Assembly_Attributes_Can_Be_Fixed(string attribute, string expected)
    {
        await CodeFixer
            .VerifyCodeFixAsync(
                $$"""
                  using System;
                  using TUnit.Core;
                  using Xunit;
                  
                  [assembly: {|#0:{{attribute}}|}]
                  namespace MyNamespace;

                  public class MyClass
                  {
                      [Test]
                      public void MyTest()
                      {
                      }
                  }
                  """,
                Verifier.Diagnostic(Rules.XunitAttributes).WithLocation(0),
                $$"""
                  using System;
                  using TUnit.Core;
                  using Xunit;

                  [assembly: {{expected}}]
                  namespace MyNamespace;
                  
                  public class MyClass
                  {
                      [Test]
                      public void MyTest()
                      {
                      }
                  }
                  """
            );
    }
}