namespace TUnit.Assertions.Tests.Bugs;

/// <summary>
/// Regression tests for GitHub issue #5702:
/// `.Member(x => x.StringProperty, p => p.IsEqualTo("..."))` incorrectly resolved the
/// collection `Member&lt;TObject, TItem&gt;` overload (TItem=char) instead of the general
/// `Member&lt;TObject, TMember&gt;` (TMember=string) overload because the collection
/// overload carries a higher `[OverloadResolutionPriority]`. The resulting
/// `IAssertionSource&lt;IEnumerable&lt;char&gt;&gt;` triggers TUnitAssertions0016 and
/// uses reference equality on strings, so non-interned but equal strings fail.
/// </summary>
public class Issue5702Tests
{
    [Test]
    public async Task Member_On_String_Property_Uses_String_Equality_Not_Reference()
    {
        var value = KeyValuePair.Create<string, object?>(NonInterned("error"), null);

        await Assert.That(value).Member(kvp => kvp.Key, key => key.IsEqualTo(NonInterned("error")));
    }

    [Test]
    public async Task Member_On_String_Property_With_Nested_Object()
    {
        var model = new Wrapper { Name = NonInterned("hello") };

        await Assert.That(model).Member(m => m.Name, n => n.IsEqualTo(NonInterned("hello")));
    }

    private static string NonInterned(string s) => new(s.ToCharArray());

    private sealed class Wrapper
    {
        public string Name { get; set; } = string.Empty;
    }
}
