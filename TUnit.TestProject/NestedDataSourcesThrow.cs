using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

public class NestedDataSourcesThrow
{
    [ClassDataSource]
    public DataSource Data { get; set; } = null!;

    [Test]
    public Task Test1()
    {
        // This test body should never execute - the test should fail during property injection
        // with "Oops something went wrong" from DataSource3's constructor
        Assert.Fail("Test body should not have executed - expected property injection to fail with 'Oops something went wrong'");
        return Task.CompletedTask;
    }

    public class DataSource : IAsyncInitializer
    {
        [ClassDataSource]
        public DataSource2 DataSource2 { get; set; } = null!;

        public bool IsInitialized { get; set; }

        public Task InitializeAsync()
        {
            IsInitialized = true;
            return Task.CompletedTask;
        }
    }

    public class DataSource2 : IAsyncInitializer
    {
        [ClassDataSource]
        public DataSource3 DataSource3 { get; set; } = null!;

        public bool IsInitialized { get; set; }

        public Task InitializeAsync()
        {
            IsInitialized = true;
            return Task.CompletedTask;
        }
    }

    public class DataSource3 : IAsyncInitializer
    {
        public DataSource3()
        {
            throw new Exception("Oops something went wrong");
        }
        public bool IsInitialized { get; set; }

        public Task InitializeAsync()
        {
            IsInitialized = true;
            return Task.CompletedTask;
        }
    }
}
