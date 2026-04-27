using System.Net;

namespace TUnit.Assertions.Tests.Bugs;

/// <summary>
/// Regression tests for GitHub issue #5765:
/// 1.40.0 introduced a second IsEqualTo overload alongside the source-generated one to
/// support wrapper Value Objects with implicit conversions (#5720). When called with a
/// same-type expected value the two overloads were equally applicable and overload
/// resolution failed with CS0121 — affecting effectively every existing test suite that
/// compared enums, primitives, value types, or records to a value of their own type.
///
/// The implicit-conversion overloads now live behind <c>#if NET9_0_OR_GREATER</c> because
/// they rely on <c>[OverloadResolutionPriority]</c> to disambiguate, and that attribute is
/// only honored by C# 13+ (which lines up with .NET 9). On older library targets the new
/// overload is omitted entirely so the source-generated overload binds without contest.
/// </summary>
public class Issue5765Tests
{
    [Test]
    public async Task IsEqualTo_SameType_Enum_Compiles_And_Passes()
    {
        var status = HttpStatusCode.OK;

        await Assert.That(status).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task IsEqualTo_SameType_Primitive_Compiles_And_Passes()
    {
        var value = 42;

        await Assert.That(value).IsEqualTo(42);
    }

    [Test]
    public async Task IsEqualTo_SameType_Record_Compiles_And_Passes()
    {
        var point = new Point(1, 2);

        await Assert.That(point).IsEqualTo(new Point(1, 2));
    }

    [Test]
    public async Task IsNotEqualTo_SameType_Enum_Compiles_And_Passes()
    {
        var status = HttpStatusCode.OK;

        await Assert.That(status).IsNotEqualTo(HttpStatusCode.NotFound);
    }

    private sealed record Point(int X, int Y);
}
