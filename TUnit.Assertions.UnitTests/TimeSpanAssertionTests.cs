using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.UnitTests;

public class TimeSpanAssertionTests
{
        [Test]
        public async Task Less_Than()
        {
            var value1 = TimeSpan.FromSeconds(1);
            var value2 = TimeSpan.FromSeconds(2);

            await TUnitAssert.That(value1).IsLessThan(value2);
        }
}