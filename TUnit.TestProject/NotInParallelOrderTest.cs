using TUnit.Core;

namespace TUnit.TestProject
{
    [NotInParallel(Order = 2)]
    public sealed class Test1
    {
        [Test]
        public async Task TestCase1()
        {
            await Task.Delay(500);
            Console.WriteLine("Test 1");
        }
    }

    [NotInParallel(Order = 1)]
    public sealed class Test2
    {
        [Test]
        public Task TestCase2()
        {
            Console.WriteLine("Test 2");
            return Task.CompletedTask;
        }
    }
}