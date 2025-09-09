using TUnit.Core.Interfaces;

namespace TUnit.UnitTests;

/// <summary>
/// Test case to reproduce the exact issue described in #1924:
/// Nested ClassDataSource property not injected in derived data source class 
/// after upgrade to 0.57.24
/// </summary>
public class NestedClassDataSourceInjectionBugTest
{
    /// <summary>
    /// Base class with ClassDataSource property (like DataClass in the issue)
    /// </summary>
    public abstract class BaseClass
    {
        [ClassDataSource<DataClass>(Shared = SharedType.PerTestSession)]
        public required DataClass TestData { get; init; }
    }

    /// <summary>
    /// Factory class that derives from BaseClass and implements IAsyncInitializer
    /// This represents the TestFactory in the issue
    /// </summary>
    public class TestFactory : BaseClass, IAsyncInitializer
    {
        public bool InitializeCalled { get; private set; }
        public bool TestDataWasNull { get; private set; }

        public Task InitializeAsync()
        {
            InitializeCalled = true;
            
            // This is where the issue occurs - TestData should not be null
            if (TestData is null)
            {
                TestDataWasNull = true;
                throw new InvalidOperationException("TestData is null - this reproduces the bug!");
            }

            // Try to access a property on TestData to confirm it's working
            var testProperty = TestData.TestProperty;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Data class that gets injected 
    /// </summary>
    public class DataClass : IAsyncInitializer
    {
        public string TestProperty { get; set; } = "test value";
        public bool IsInitialized { get; private set; }

        public Task InitializeAsync()
        {
            IsInitialized = true;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Test class that uses the TestFactory
    /// </summary>
    public class Tests : IAsyncInitializer
    {
        [ClassDataSource<TestFactory>(Shared = SharedType.PerTestSession)]
        public required TestFactory TestDataFactory { get; init; }

        public Task InitializeAsync()
        {
            // TestDataFactory should be injected and initialized properly
            return Task.CompletedTask;
        }

        [Test]
        public async Task TestFactory_Should_Have_TestData_Injected()
        {
            // Verify the factory was initialized
            await Assert.That(TestDataFactory.InitializeCalled).IsTrue();
            
            // Verify TestData was not null during initialization (the bug)
            await Assert.That(TestDataFactory.TestDataWasNull).IsFalse();
            
            // Verify TestData is accessible
            await Assert.That(TestDataFactory.TestData).IsNotNull();
            await Assert.That(TestDataFactory.TestData.IsInitialized).IsTrue();
            await Assert.That(TestDataFactory.TestData.TestProperty).IsEqualTo("test value");
        }
    }
}