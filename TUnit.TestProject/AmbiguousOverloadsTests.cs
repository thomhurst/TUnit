using System.Diagnostics.CodeAnalysis;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

[SuppressMessage("Usage", "TUnitAssertions0005:Assert.That(...) should not be used with a constant value")]
public class AmbiguousOverloadsTests
{
    [Test]
    public async Task Test()
    {
        await Assert.That(1).IsEqualTo(1);
        await Assert.That(1d).IsEqualTo(1d);
        await Assert.That("1").IsEqualTo("1");
        await Assert.That(true).IsEqualTo(true);
        await Assert.That('1').IsEqualTo('1');
        await Assert.That(TimeSpan.FromSeconds(1)).IsEqualTo(TimeSpan.FromSeconds(1));
        await Assert.That(new DateTime(2000,1,1)).IsEqualTo(new DateTime(2000,1,1));
        await Assert.That(new object()).IsEqualTo(new object());
        await Assert.That(new MyStruct()).IsEqualTo(new MyStruct());
    }

    private struct MyStruct;
}