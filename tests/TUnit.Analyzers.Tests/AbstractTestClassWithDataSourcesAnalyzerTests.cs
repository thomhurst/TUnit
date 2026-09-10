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
    public async Task No_Warning_For_Abstract_Class_With_MethodDataSource_When_No_Subclasses()
    {
        // No warning when there are no subclasses - the abstract class may be in a library
        // meant to be subclassed by consuming assemblies
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System.Collections.Generic;

                public abstract class AbstractTestBase
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
    public async Task No_Warning_For_Abstract_Class_With_InstanceMethodDataSource_When_No_Subclasses()
    {
        // No warning when there are no subclasses - the abstract class may be in a library
        // meant to be subclassed by consuming assemblies
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System.Collections.Generic;

                public abstract class ServiceCollectionTest
                {
                    public IEnumerable<int> SingletonServices() => new[] { 1, 2, 3 };

                    [Test]
                    [InstanceMethodDataSource(nameof(SingletonServices))]
                    public void ServiceCanBeCreatedAsSingleton(int value)
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task No_Warning_For_Abstract_Class_With_Arguments_When_No_Subclasses()
    {
        // No warning when there are no subclasses - the abstract class may be in a library
        // meant to be subclassed by consuming assemblies
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public abstract class AbstractTestBase
                {
                    [Test]
                    [Arguments(1)]
                    [Arguments(2)]
                    public void DataDrivenTest(int value)
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task No_Warning_For_Abstract_Class_With_ClassDataSource_When_No_Subclasses()
    {
        // No warning when there are no subclasses - the abstract class may be in a library
        // meant to be subclassed by consuming assemblies
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class TestData
                {
                }

                public abstract class AbstractTestBase
                {
                    [Test]
                    [ClassDataSource<TestData>]
                    public void DataDrivenTest(TestData data)
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task No_Warning_When_Concrete_Class_With_InheritsTests_Exists()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                [InheritsTests]
                public class Tests1 : Tests { }

                [InheritsTests]
                public class Tests2 : Tests { }

                public abstract class Tests
                {
                    [Test]
                    [Arguments(true)]
                    [Arguments(false)]
                    public void TestName(bool flag) { }

                    [Test]
                    public void TestName2() { }
                }
                """
            );
    }

    [Test]
    public async Task No_Warning_When_Single_Concrete_Class_With_InheritsTests_Exists()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System.Collections.Generic;

                [InheritsTests]
                public class ConcreteTest : AbstractTestBase { }

                public abstract class AbstractTestBase
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
    public async Task Warning_When_Concrete_Class_Exists_But_No_InheritsTests()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class Tests1 : Tests { }

                public abstract class {|#0:Tests|}
                {
                    [Test]
                    [Arguments(true)]
                    [Arguments(false)]
                    public void TestName(bool flag) { }
                }
                """,

                Verifier.Diagnostic(Rules.AbstractTestClassWithDataSources)
                    .WithLocation(0)
                    .WithArguments("Tests")
            );
    }

    [Test]
    public async Task Warning_When_Multiple_Concrete_Classes_Exist_But_None_Have_InheritsTests()
    {
        // When there are multiple subclasses but none have [InheritsTests], warn
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class Tests1 : Tests { }
                public class Tests2 : Tests { }
                public class Tests3 : Tests { }

                public abstract class {|#0:Tests|}
                {
                    [Test]
                    [Arguments(true)]
                    [Arguments(false)]
                    public void TestName(bool flag) { }
                }
                """,

                Verifier.Diagnostic(Rules.AbstractTestClassWithDataSources)
                    .WithLocation(0)
                    .WithArguments("Tests")
            );
    }

    [Test]
    public async Task No_Warning_When_At_Least_One_Concrete_Class_Has_InheritsTests()
    {
        // When at least one subclass has [InheritsTests], no warning
        // (even if other subclasses don't have it)
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class Tests1 : Tests { }

                [InheritsTests]
                public class Tests2 : Tests { }

                public class Tests3 : Tests { }

                public abstract class Tests
                {
                    [Test]
                    [Arguments(true)]
                    [Arguments(false)]
                    public void TestName(bool flag) { }
                }
                """
            );
    }
}
