// Assembly-level retry should be overridden by class and method level
[assembly: Retry(5)]

namespace TUnit.TestProject
{
    // Class-level retry should override assembly but be overridden by method level
    [Retry(3), NotInParallel(nameof(ScopedRetryAttributePriorityTests))]
    public class ScopedRetryAttributePriorityTests
    {
        public static int MethodLevelRetryCount { get; private set; }
        public static int ClassLevelRetryCount { get; private set; }
        public static int AssemblyLevelRetryCount { get; private set; }
        
        [Test]
        [Retry(1)] // Method level - should take precedence
        public void TestWithMethodLevelRetry()
        {
            MethodLevelRetryCount++;
            throw new Exception("Expected failure");
        }
        
        [Test] // Should use class-level retry (3)
        public void TestWithClassLevelRetry()
        {
            ClassLevelRetryCount++;
            throw new Exception("Expected failure");
        }
        
        [Test]
        [DependsOn(nameof(TestWithMethodLevelRetry), ProceedOnFailure = true)]
        [DependsOn(nameof(TestWithClassLevelRetry), ProceedOnFailure = true)]
        public async Task VerifyRetryCounts()
        {
            
            // Method-level retry should have run 2 times (initial + 1 retry)
            await Assert.That(MethodLevelRetryCount).IsEqualTo(2, 
                "Method-level retry attribute (1) should override class-level (3) and assembly-level (5)");
            
            // Class-level retry should have run 4 times (initial + 3 retries)  
            await Assert.That(ClassLevelRetryCount).IsEqualTo(4,
                "Class-level retry attribute (3) should override assembly-level (5)");
        }
    }
    
    // A class without any retry attribute should use assembly-level
    [NotInParallel("AssemblyLevelRetryTest")]
    public class AssemblyLevelRetryTest
    {
        public static int RetryCount { get; private set; }
        
        [Test]
        public void TestWithAssemblyLevelRetry()
        {
            AssemblyLevelRetryTest.RetryCount++;
            throw new Exception("Expected failure");
        }
        
        [Test]
        [DependsOn(nameof(TestWithAssemblyLevelRetry), ProceedOnFailure = true)]
        public async Task VerifyAssemblyLevelRetryCount()
        {
            // Should use assembly-level retry (5) - run 6 times (initial + 5 retries)
            await Assert.That(RetryCount).IsEqualTo(6,
                "Assembly-level retry attribute (5) should be used when no class or method level is present");
        }
    }
}