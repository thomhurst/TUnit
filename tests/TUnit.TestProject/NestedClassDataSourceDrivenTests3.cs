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

    public class MyDataProvider() : DataSourceGeneratorAttribute<SomeClass1>, IAsyncInitializer
    {
        [MyDataProvider1]
        public SomeClass1? InnerClass { get; init; }

        protected override IEnumerable<Func<SomeClass1>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
        {
            var innerClass = InnerClass ?? throw new InvalidOperationException("InnerClass was not injected.");

            yield return () => innerClass;
        }

        public async Task InitializeAsync()
        {
            var innerClass = InnerClass ?? throw new InvalidOperationException("InnerClass was not injected.");

            await Assert.That(innerClass).IsNotNull();
            await Assert.That(innerClass.IsInitialized).IsTrue();
            await Assert.That(innerClass.InitializedCount).IsEqualTo(1);

            await Assert.That(innerClass.InnerClass).IsNotNull();
            await Assert.That(innerClass.InnerClass.IsInitialized).IsTrue();
            await Assert.That(innerClass.InnerClass.InitializedCount).IsEqualTo(1);

            await Assert.That(innerClass.InnerClass.InnerClass).IsNotNull();
            await Assert.That(innerClass.InnerClass.InnerClass.IsInitialized).IsTrue();
            await Assert.That(innerClass.InnerClass.InnerClass.InitializedCount).IsEqualTo(1);
        }
    }

    public class MyDataProvider1() : DataSourceGeneratorAttribute<SomeClass1>, IAsyncInitializer
    {
        [MyDataProvider2]
        public SomeClass2? InnerClass { get; init; }

        protected override IEnumerable<Func<SomeClass1>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
        {
            var innerClass = InnerClass ?? throw new InvalidOperationException("InnerClass was not injected.");

            yield return () =>
            {
                if (innerClass is null)
                {
                    throw new InvalidOperationException("innerClass was not set.");
                }

                if (innerClass.InnerClass is null)
                {
                    throw new InvalidOperationException("innerClass.InnerClass was not set.");
                }

                return new SomeClass1
                {
                    InnerClass = innerClass
                };
            };
        }

        public async Task InitializeAsync()
        {
            var innerClass = InnerClass ?? throw new InvalidOperationException("InnerClass was not injected.");

            await Assert.That(innerClass).IsNotNull();
            await Assert.That(innerClass.IsInitialized).IsTrue();
            await Assert.That(innerClass.InitializedCount).IsEqualTo(1);

            await Assert.That(innerClass.InnerClass).IsNotNull();
            await Assert.That(innerClass.InnerClass.IsInitialized).IsTrue();
            await Assert.That(innerClass.InnerClass.InitializedCount).IsEqualTo(1);
        }
    }

    public class MyDataProvider2() : DataSourceGeneratorAttribute<SomeClass2>, IAsyncInitializer
    {
        [MyDataProvider3]
        public SomeClass3? InnerClass { get; init; }

        protected override IEnumerable<Func<SomeClass2>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
        {
            var innerClass = InnerClass ?? throw new InvalidOperationException("InnerClass was not injected.");

            yield return () =>
            {
                if (innerClass is null)
                {
                    throw new InvalidOperationException("innerClass was not set.");
                }

                return new SomeClass2
                {
                    InnerClass = innerClass
                };
            };
        }

        public async Task InitializeAsync()
        {
            var innerClass = InnerClass ?? throw new InvalidOperationException("InnerClass was not injected.");

            await Assert.That(innerClass).IsNotNull();
            await Assert.That(innerClass.IsInitialized).IsTrue();
            await Assert.That(innerClass.InitializedCount).IsEqualTo(1);
        }
    }

    public class MyDataProvider3() : DataSourceGeneratorAttribute<SomeClass3>, IAsyncInitializer
    {
        [ClassDataSource<SomeClass3>(Shared = SharedType.PerTestSession)]
        public SomeClass3? InnerClass { get; init; }

        protected override IEnumerable<Func<SomeClass3>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
        {
            var innerClass = InnerClass ?? throw new InvalidOperationException("InnerClass was not injected.");

            yield return () => innerClass;
        }

        public async Task InitializeAsync()
        {
            var innerClass = InnerClass ?? throw new InvalidOperationException("InnerClass was not injected.");

            await Assert.That(innerClass).IsNotNull();
            await Assert.That(innerClass.IsInitialized).IsTrue();
            await Assert.That(innerClass.InitializedCount).IsEqualTo(1);
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
