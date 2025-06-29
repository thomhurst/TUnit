using System.Diagnostics.CodeAnalysis;
using TUnit.TestProject.Attributes;
using TUnit.TestProject.Models;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class NestedClassDataSourceDrivenTests2
{
    [Test]
    [MyDataProvider]
    [Repeat(5)]
    public async Task DataSource_Class(SomeClass1 value)
    {
        Console.WriteLine(value);

        await Assert.That(value.IsInitialized).IsTrue();
        await Assert.That(value.InitializedCount).IsEqualTo(1);
        await Assert.That(value.InnerClass.IsInitialized).IsTrue();
        await Assert.That(value.InnerClass.InitializedCount).IsEqualTo(1);
        await Assert.That(value.InnerClass.InnerClass.IsInitialized).IsTrue();
        await Assert.That(value.InnerClass.InnerClass.InitializedCount).IsEqualTo(1);

        await Assert.That(value.IsDisposed).IsFalse();
        await Assert.That(value.InnerClass.IsDisposed).IsFalse();
        await Assert.That(value.InnerClass.InnerClass.IsDisposed).IsFalse();
    }

    [method: SetsRequiredMembers]
    public class MyDataProvider() : DataSourceGeneratorAttribute<SomeClass1>
    {
        [ClassDataSource<SomeClass1>]
        public required SomeClass1 InnerClass { get; init; }

        protected override IEnumerable<Func<SomeClass1>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
        {
            yield return () => InnerClass;
        }
    }

    public record SomeClass1 : InitialisableClass
    {
        [ClassDataSource<SomeClass2>(Shared = SharedType.PerAssembly)]
        public required SomeClass2 InnerClass { get; init; }

        public override async Task InitializeAsync()
        {
            await Assert.That(InnerClass.IsInitialized).IsTrue();
            await base.InitializeAsync();
        }
    }

    public record SomeClass2 : InitialisableClass
    {
        [ClassDataSource<SomeClass3>(Shared = SharedType.PerAssembly)]
        public required SomeClass3 InnerClass { get; init; }

        public override async Task InitializeAsync()
        {
            await Assert.That(InnerClass.IsInitialized).IsTrue();
            await base.InitializeAsync();
        }
    }

    public record SomeClass3 : InitialisableClass;
}
