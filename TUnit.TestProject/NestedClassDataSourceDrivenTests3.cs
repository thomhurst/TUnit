using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;
using TUnit.TestProject.Models;

namespace TUnit.TestProject.NestedClassDataSourceDrivenTests3;

[EngineTest(ExpectedResult.Pass)]
public class NestedClassDataSourceDrivenTests3
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
    public class MyDataProvider() : DataSourceGeneratorAttribute<SomeClass1>, IAsyncInitializer
    {
        [MyDataProvider1]
        public required SomeClass1 InnerClass { get; init; }

        protected override IEnumerable<Func<SomeClass1>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
        {
            yield return () => InnerClass;
        }

        public async Task InitializeAsync()
        {
            await Assert.That(InnerClass).IsNotNull();
            await Assert.That(InnerClass.IsInitialized).IsTrue();
            await Assert.That(InnerClass.InitializedCount).IsEqualTo(1);

            await Assert.That(InnerClass.InnerClass).IsNotNull();
            await Assert.That(InnerClass.InnerClass.IsInitialized).IsTrue();
            await Assert.That(InnerClass.InnerClass.InitializedCount).IsEqualTo(1);

            await Assert.That(InnerClass.InnerClass.InnerClass).IsNotNull();
            await Assert.That(InnerClass.InnerClass.InnerClass.IsInitialized).IsTrue();
            await Assert.That(InnerClass.InnerClass.InnerClass.InitializedCount).IsEqualTo(1);
        }
    }

    [method: SetsRequiredMembers]
    public class MyDataProvider1() : DataSourceGeneratorAttribute<SomeClass1>, IAsyncInitializer
    {
        [MyDataProvider2]
        public required SomeClass2 InnerClass { get; init; }

        protected override IEnumerable<Func<SomeClass1>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
        {
            yield return () =>
            {
                if (InnerClass is null)
                {
                    throw new InvalidOperationException("InnerClass was not set.");
                }

                if (InnerClass.InnerClass is null)
                {
                    throw new InvalidOperationException("InnerClass.InnerClass was not set.");
                }

                return new SomeClass1
                {
                    InnerClass = InnerClass
                };
            };
        }

        public async Task InitializeAsync()
        {
            await Assert.That(InnerClass).IsNotNull();
            await Assert.That(InnerClass.IsInitialized).IsTrue();
            await Assert.That(InnerClass.InitializedCount).IsEqualTo(1);

            await Assert.That(InnerClass.InnerClass).IsNotNull();
            await Assert.That(InnerClass.InnerClass.IsInitialized).IsTrue();
            await Assert.That(InnerClass.InnerClass.InitializedCount).IsEqualTo(1);
        }
    }

    [method: SetsRequiredMembers]
    public class MyDataProvider2() : DataSourceGeneratorAttribute<SomeClass2>, IAsyncInitializer
    {
        [MyDataProvider3]
        public required SomeClass3 InnerClass { get; init; }

        protected override IEnumerable<Func<SomeClass2>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
        {
            yield return () =>
            {
                if (InnerClass is null)
                {
                    throw new InvalidOperationException("InnerClass was not set.");
                }

                return new SomeClass2
                {
                    InnerClass = InnerClass
                };
            };
        }

        public async Task InitializeAsync()
        {
            await Assert.That(InnerClass).IsNotNull();
            await Assert.That(InnerClass.IsInitialized).IsTrue();
            await Assert.That(InnerClass.InitializedCount).IsEqualTo(1);
        }
    }

    [method: SetsRequiredMembers]
    public class MyDataProvider3() : DataSourceGeneratorAttribute<SomeClass3>, IAsyncInitializer
    {
        [ClassDataSource<SomeClass3>(Shared = SharedType.PerTestSession)]
        public required SomeClass3 InnerClass { get; init; }

        protected override IEnumerable<Func<SomeClass3>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
        {
            yield return () => InnerClass;
        }

        public async Task InitializeAsync()
        {
            await Assert.That(InnerClass).IsNotNull();
            await Assert.That(InnerClass.IsInitialized).IsTrue();
            await Assert.That(InnerClass.InitializedCount).IsEqualTo(1);
        }
    }

    public record SomeClass1 : InitialisableClass
    {
        public required SomeClass2 InnerClass { get; init; }

        public override async Task InitializeAsync()
        {
            await Assert.That(InnerClass.IsInitialized).IsTrue();
            await base.InitializeAsync();
        }
    }

    public record SomeClass2 : InitialisableClass
    {
        public required SomeClass3 InnerClass { get; init; }

        public override async Task InitializeAsync()
        {
            await Assert.That(InnerClass.IsInitialized).IsTrue();
            await base.InitializeAsync();
        }
    }

    public record SomeClass3 : InitialisableClass;
}
