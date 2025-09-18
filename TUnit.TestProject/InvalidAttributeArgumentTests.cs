using TUnit.Core;

namespace TUnit.TestProject
{
    public class InvalidAttributeArgumentTests
    {
        [Test]
        [MethodDataSource("ValidMethodName")]  // Start with a working case
        public async Task TestWithValidMethodName(int value)
        {
            // Test implementation would go here
        }
        
        [Test]
        [MethodDataSource(nameof())]  // This should cause the error - empty nameof()
        public async Task TestWithEmptyNameof(int value)
        {
            // Test implementation would go here
        }
        
        public static int[] ValidMethodName()
        {
            return new[] { 1, 2, 3 };
        }
    }
}