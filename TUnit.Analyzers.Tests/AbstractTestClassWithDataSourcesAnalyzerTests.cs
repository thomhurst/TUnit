using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.AbstractTestClassWithDataSourcesAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class AbstractTestClassWithDataSourcesAnalyzerTests
{
    [Test]
    public async Task No_Warning_For_Concrete_Class_With_Data_Source()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System.Collections.Generic;

                public class ConcreteTests
                {
                    public static IEnumerable<int> TestData() => new[] { 1, 2, 3 };

                    [Test]
                    [MethodDataSource(nameof(TestData))]
                    public void DataDrivenTest(int value)
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task No_Warning_For_Abstract_Class_Without_Data_Sources()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public abstract class AbstractTestBase
                {
                    [Test]
                    public void SimpleTest()
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task No_Warning_For_Abstract_Class_Without_Tests()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public abstract class AbstractBase
                {
                    public void HelperMethod()
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task Warning_For_Abstract_Class_With_MethodDataSource()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System.Collections.Generic;

                public abstract class {|#0:AbstractTestBase|}
                {
                    public static IEnumerable<int> TestData() => new[] { 1, 2, 3 };

                    [Test]
                    [MethodDataSource(nameof(TestData))]
                    public void DataDrivenTest(int value)
                    {
                    }
                }
                """,

                Verifier.Diagnostic(Rules.AbstractTestClassWithDataSources)
                    .WithLocation(0)
                    .WithArguments("AbstractTestBase")
            );
    }

    [Test]
    public async Task Warning_For_Abstract_Class_With_InstanceMethodDataSource()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System.Collections.Generic;

                public abstract class {|#0:ServiceCollectionTest|}
                {
                    public IEnumerable<int> SingletonServices() => new[] { 1, 2, 3 };

                    [Test]
                    [InstanceMethodDataSource(nameof(SingletonServices))]
                    public void ServiceCanBeCreatedAsSingleton(int value)
                    {
                    }
                }
                """,

                Verifier.Diagnostic(Rules.AbstractTestClassWithDataSources)
                    .WithLocation(0)
                    .WithArguments("ServiceCollectionTest")
            );
    }

    [Test]
    public async Task Warning_For_Abstract_Class_With_Arguments()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public abstract class {|#0:AbstractTestBase|}
                {
                    [Test]
                    [Arguments(1)]
                    [Arguments(2)]
                    public void DataDrivenTest(int value)
                    {
                    }
                }
                """,

                Verifier.Diagnostic(Rules.AbstractTestClassWithDataSources)
                    .WithLocation(0)
                    .WithArguments("AbstractTestBase")
            );
    }

    [Test]
    public async Task Warning_For_Abstract_Class_With_ClassDataSource()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class TestData
                {
                }

                public abstract class {|#0:AbstractTestBase|}
                {
                    [Test]
                    [ClassDataSource<TestData>]
                    public void DataDrivenTest(TestData data)
                    {
                    }
                }
                """,

                Verifier.Diagnostic(Rules.AbstractTestClassWithDataSources)
                    .WithLocation(0)
                    .WithArguments("AbstractTestBase")
            );
    }
}
