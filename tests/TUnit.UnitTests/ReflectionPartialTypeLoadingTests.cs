using System.Reflection;
using TUnit.Engine.Discovery;

namespace TUnit.UnitTests;

public class ReflectionPartialTypeLoadingTests
{
    [Test]
    public async Task Partial_Type_Loading_Preserves_Builders_And_Caches_Loadable_Types()
    {
        var loaderException = new FileNotFoundException("An unrelated dependency is unavailable");
        var assembly = new FailingAssembly(new ReflectionTypeLoadException(
            [null, typeof(LoadableBuilder), null, typeof(ReflectionPartialTypeLoadingTests)],
            [loaderException, loaderException]));

        var types = ReflectionTestDataCollector.GetAssemblyTypes(assembly);

        // Ordinary non-builder types must survive partial loading too.
        await Assert.That(types).IsEquivalentTo(new[] { typeof(LoadableBuilder), typeof(ReflectionPartialTypeLoadingTests) });
        await Assert.That(ReflectionTestDataCollector.GetAssemblyTypes(assembly)).IsSameReferenceAs(types);
        await Assert.That(assembly.GetTypesCalls).IsEqualTo(1);
#pragma warning disable TUnitWIP0001
        await Assert.That(types.SelectMany(type => type.GetMethods())
            .Count(method => method.IsDefined(typeof(DynamicTestBuilderAttribute), false))).IsEqualTo(1);
#pragma warning restore TUnitWIP0001
    }

    [Test]
    public async Task Dynamic_Discovery_Preserves_Tests_From_Partially_Loaded_Assembly()
    {
        // A fresh assembly ensures ordinary discovery has not already primed the type cache.
        var assembly = new FailingAssembly(new ReflectionTypeLoadException(
            [null, typeof(LoadableBuilder)], [new FileNotFoundException("Dependency unavailable")]));

        var tests = await new ReflectionTestDataCollector().DiscoverDynamicTests(
            Guid.NewGuid().ToString(), [assembly]);

        var matchingTests = tests.Where(test => test.TestClassType == typeof(LoadableBuilder)
            && test.TestMethodName == nameof(LoadableBuilder.ContributedTest)).ToArray();
        await Assert.That(matchingTests).HasCount().EqualTo(1);
        await Assert.That(assembly.GetTypesCalls).IsEqualTo(1);
    }

    [Test]
    public async Task Complete_Type_Loading_Failure_Returns_No_Types()
    {
        var assembly = new FailingAssembly(new ReflectionTypeLoadException(
            [null], [new FileNotFoundException("Dependency unavailable")]));

        await Assert.That(ReflectionTestDataCollector.GetAssemblyTypes(assembly)).IsEmpty();
    }

    private sealed class FailingAssembly(Exception exception) : Assembly
    {
        public int GetTypesCalls { get; private set; }

        public override AssemblyName GetName() => new("PartialLoadingTestAssembly");

        public override AssemblyName[] GetReferencedAssemblies() => [typeof(TestAttribute).Assembly.GetName()];

        public override Type[] GetTypes()
        {
            GetTypesCalls++;
            throw exception;
        }
    }

    internal sealed class LoadableBuilder
    {
#pragma warning disable TUnitWIP0001
        [DynamicTestBuilder]
#pragma warning restore TUnitWIP0001
        public static void Build(DynamicTestBuilderContext context)
        {
            context.AddTest(new DynamicTest<LoadableBuilder>
            {
                TestMethod = test => test.ContributedTest(),
                TestMethodArguments = [],
                Attributes = []
            });
        }

        public void ContributedTest()
        {
        }
    }
}
