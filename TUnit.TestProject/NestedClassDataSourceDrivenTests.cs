using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Library.Models;

namespace TUnit.TestProject;

public class NestedClassDataSourceDrivenTests
{
    [Test]
    [ClassDataSource<SomeClass1>]
    public async Task DataSource_Class(SomeClass1 value)
    {
        await Assert.That(value.IsInitialized).IsTrue();
        await Assert.That(value.InnerClass.IsInitialized).IsTrue();
        await Assert.That(value.InnerClass.InnerClass.IsInitialized).IsTrue();
    }

    public class SomeClass1 : IAsyncInitializer
    {
        [ClassDataSource<SomeClass2>(Shared = SharedType.PerAssembly)]
        public required SomeClass2 InnerClass { get; init; }
        
        public Task InitializeAsync()
        {
            IsInitialized = true;
            return Task.CompletedTask;
        }

        public bool IsInitialized
        {
            get;
            private set;
        }
    }
    
    public class SomeClass2 : IAsyncInitializer
    {
        [ClassDataSource<SomeClass3>(Shared = SharedType.PerAssembly)]
        public required SomeClass3 InnerClass { get; init; }
        
        public Task InitializeAsync()
        {
            IsInitialized = true;
            return Task.CompletedTask;
        }

        public bool IsInitialized
        {
            get;
            private set;
        }
    }
    
    public class SomeClass3 : IAsyncInitializer
    {
        public Task InitializeAsync()
        {
            IsInitialized = true;
            return Task.CompletedTask;
        }

        public bool IsInitialized
        {
            get;
            private set;
        }
    }
}