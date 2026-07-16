using System.Threading.Tasks;
using TUnit.Core;

namespace VerifyFix
{
    public sealed class VerifyFixTest
    {
        [Test]
        [Arguments("test")]
        [Arguments("hello")]
        public async Task TestGenericMethod<T>(T value) where T : class
        {
            // This test should compile successfully with our fix
            await Task.CompletedTask;
        }
    }
}