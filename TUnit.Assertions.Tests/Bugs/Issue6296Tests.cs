namespace TUnit.Assertions.Tests.Bugs;

/// <summary>
/// Regression tests for GitHub issue #6296:
/// <c>await Assert.That(guid).IsEqualTo(guid)</c> failed to compile with
/// <c>CS0121: The call is ambiguous between IsEqualTo&lt;TValue, TOther&gt; and
/// IsEqualTo&lt;TValue&gt;</c> for consumers building with the .NET 8 SDK.
///
/// The cross-type <c>IsEqualTo&lt;TValue, TOther&gt;</c> overload (added for value-object
/// ergonomics, see <see cref="Issue5720Tests"/>) is equally applicable to the same-type
/// overload when the expected value is the same type as the source (Guid vs Guid). It was
/// previously deprioritized only by <c>[OverloadResolutionPriority]</c>, which is honored
/// solely by the Roslyn that ships with the .NET 9 SDK and later — NOT by LangVersion. The
/// .NET 8 SDK's compiler ignores it, so the call stayed ambiguous regardless of LangVersion.
///
/// The fix gives the cross-type overloads a trailing <c>params object[]</c>, making them
/// applicable only in expanded form so they lose to the normal-form same-type overload via
/// the compiler-version-independent "normal beats expanded" tie-break. The body of each test
/// below would not compile before the fix when built with the .NET 8 SDK — successful
/// compilation IS the regression assertion; the runtime assertions confirm the same-type
/// overload (default equality), not the implicit-conversion overload, is the one selected.
/// </summary>
public class Issue6296Tests
{
    [Test]
    public async Task Guid_IsEqualTo_SameType_IsNotAmbiguous()
    {
        var value = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var same = Guid.Parse("11111111-1111-1111-1111-111111111111");

        await Assert.That(value).IsEqualTo(same);
    }

    [Test]
    public async Task Guid_IsNotEqualTo_SameType_IsNotAmbiguous()
    {
        var value = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var other = Guid.Parse("22222222-2222-2222-2222-222222222222");

        await Assert.That(value).IsNotEqualTo(other);
    }

    [Test]
    public async Task Int_IsEqualTo_SameType_IsNotAmbiguous()
    {
        await Assert.That(42).IsEqualTo(42);
    }

    [Test]
    public async Task DateTime_IsEqualTo_SameType_IsNotAmbiguous()
    {
        var now = new DateTime(2026, 6, 24, 0, 0, 0, DateTimeKind.Utc);

        await Assert.That(now).IsEqualTo(now);
    }

    [Test]
    public async Task Guid_IsEqualTo_SameType_Fails_When_Different()
    {
        var value = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var other = Guid.Parse("22222222-2222-2222-2222-222222222222");

        await Assert.That(async () => await Assert.That(value).IsEqualTo(other))
            .Throws<AssertionException>();
    }
}
