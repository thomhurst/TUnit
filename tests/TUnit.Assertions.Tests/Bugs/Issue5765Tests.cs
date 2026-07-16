using System.Net;

namespace TUnit.Assertions.Tests.Bugs;

public class Issue5765Tests
{
    [Test]
    public async Task IsEqualTo_SameType_Enum()
    {
        var status = HttpStatusCode.OK;

        await Assert.That(status).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task IsEqualTo_SameType_Primitive()
    {
        var value = 42;

        await Assert.That(value).IsEqualTo(42);
    }

    [Test]
    public async Task IsEqualTo_SameType_Record()
    {
        var point = new Point(1, 2);

        await Assert.That(point).IsEqualTo(new Point(1, 2));
    }

    [Test]
    public async Task IsNotEqualTo_SameType_Enum()
    {
        var status = HttpStatusCode.OK;

        await Assert.That(status).IsNotEqualTo(HttpStatusCode.NotFound);
    }

    private sealed record Point(int X, int Y);
}
