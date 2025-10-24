namespace TUnit.Assertions.Tests;

/// <summary>
/// Tests for Member assertions with collection types (arrays, lists, dictionaries).
/// Addresses issue #3495 where collection assertion methods were not available within Member lambdas.
/// </summary>
public class MemberCollectionAssertionTests
{
    [Test]
    public async Task Member_Array_HasCount_Passes()
    {
        var obj = new TestClass
        {
            Tags = ["tag1", "tag2", "tag3"]
        };

        await Assert.That(obj)
            .Member(x => x.Tags, tags => tags.HasCount(3));
    }

    [Test]
    public async Task Member_Array_HasCount_Fails()
    {
        var obj = new TestClass
        {
            Tags = ["tag1", "tag2"]
        };

        var exception = await Assert.ThrowsAsync<TUnitAssertionException>(async () =>
            await Assert.That(obj).Member(x => x.Tags, tags => tags.HasCount(5)));

        await Assert.That(exception.Message).Contains("to have count 5");
        await Assert.That(exception.Message).Contains("but found 2");
    }

    [Test]
    public async Task Member_Array_Contains_Passes()
    {
        var obj = new TestClass
        {
            Tags = ["tag1", "tag2", "tag3"]
        };

        await Assert.That(obj)
            .Member(x => x.Tags, tags => tags.Contains("tag2"));
    }

    [Test]
    public async Task Member_Array_Contains_Fails()
    {
        var obj = new TestClass
        {
            Tags = ["tag1", "tag2"]
        };

        var exception = await Assert.ThrowsAsync<TUnitAssertionException>(async () =>
            await Assert.That(obj).Member(x => x.Tags, tags => tags.Contains("missing")));

        await Assert.That(exception.Message).Contains("to contain");
        await Assert.That(exception.Message).Contains("missing");
    }

    [Test]
    public async Task Member_Array_IsEmpty_Passes()
    {
        var obj = new TestClass
        {
            Tags = []
        };

        await Assert.That(obj)
            .Member(x => x.Tags, tags => tags.IsEmpty());
    }

    [Test]
    public async Task Member_Array_IsEmpty_Fails()
    {
        var obj = new TestClass
        {
            Tags = ["tag1"]
        };

        var exception = await Assert.ThrowsAsync<TUnitAssertionException>(async () =>
            await Assert.That(obj).Member(x => x.Tags, tags => tags.IsEmpty()));

        await Assert.That(exception.Message).Contains("to be empty");
    }

    [Test]
    public async Task Member_Array_IsNotEmpty_Passes()
    {
        var obj = new TestClass
        {
            Tags = ["tag1"]
        };

        await Assert.That(obj)
            .Member(x => x.Tags, tags => tags.IsNotEmpty());
    }

    [Test]
    public async Task Member_Array_Chained_Assertions_Passes()
    {
        var obj = new TestClass
        {
            Tags = ["tag1", "tag2"]
        };

        await Assert.That(obj)
            .Member(x => x.Tags, tags => tags.HasCount(2).And.Contains("tag1").And.Contains("tag2"));
    }

    [Test]
    public async Task Member_Array_Chained_With_Parent_IsNotNull()
    {
        var obj = new TestClass
        {
            Tags = ["tag1"]
        };

        await Assert.That(obj)
            .IsNotNull()
            .And.Member(x => x.Tags, tags => tags.HasCount(1).And.Contains("tag1"));
    }

    [Test]
    public async Task Member_List_HasCount_Passes()
    {
        var obj = new TestClass
        {
            Items = new List<int> { 1, 2, 3 }
        };

        await Assert.That(obj)
            .Member(x => x.Items, items => items.HasCount(3));
    }

    [Test]
    public async Task Member_List_Contains_Passes()
    {
        var obj = new TestClass
        {
            Items = new List<int> { 1, 2, 3 }
        };

        await Assert.That(obj)
            .Member(x => x.Items, items => items.Contains(2));
    }

    [Test]
    public async Task Member_List_All_Predicate_Passes()
    {
        var obj = new TestClass
        {
            Items = new List<int> { 2, 4, 6 }
        };

        await Assert.That(obj)
            .Member(x => x.Items, items => items.All(x => x % 2 == 0));
    }

    [Test]
    public async Task Member_List_Any_Predicate_Passes()
    {
        var obj = new TestClass
        {
            Items = new List<int> { 1, 2, 3 }
        };

        await Assert.That(obj)
            .Member(x => x.Items, items => items.Any(x => x > 2));
    }

    [Test]
    public async Task Member_Dictionary_IsEmpty_Passes()
    {
        var obj = new TestClass
        {
            Attributes = new Dictionary<string, string>()
        };

        await Assert.That(obj)
            .Member(x => x.Attributes, attrs => attrs.IsEmpty());
    }

    [Test]
    public async Task Member_Dictionary_IsEmpty_Fails()
    {
        var obj = new TestClass
        {
            Attributes = new Dictionary<string, string> { ["key"] = "value" }
        };

        var exception = await Assert.ThrowsAsync<TUnitAssertionException>(async () =>
            await Assert.That(obj).Member(x => x.Attributes, attrs => attrs.IsEmpty()));

        await Assert.That(exception.Message).Contains("to be empty");
    }

    [Test]
    public async Task Member_Dictionary_HasCount_Passes()
    {
        var obj = new TestClass
        {
            Attributes = new Dictionary<string, string> { ["key1"] = "value1", ["key2"] = "value2" }
        };

        await Assert.That(obj)
            .Member(x => x.Attributes, attrs => attrs.HasCount(2));
    }

    [Test]
    public async Task Member_Dictionary_ContainsKey_Passes()
    {
        var obj = new TestClass
        {
            Attributes = new Dictionary<string, string> { ["status"] = "active", ["priority"] = "high" }
        };

        await Assert.That(obj)
            .Member(x => x.Attributes, attrs => attrs.ContainsKey("status"));
    }

    [Test]
    public async Task Member_Dictionary_ContainsKey_And_IsNotEmpty()
    {
        var obj = new TestClass
        {
            Attributes = new Dictionary<string, string> { ["status"] = "active" }
        };

        await Assert.That(obj)
            .Member(x => x.Attributes, attrs => attrs.ContainsKey("status").And.IsNotEmpty());
    }

    [Test]
    public async Task Member_Dictionary_DoesNotContainKey_Passes()
    {
        var obj = new TestClass
        {
            Attributes = new Dictionary<string, string> { ["key1"] = "value1" }
        };

        await Assert.That(obj)
            .Member(x => x.Attributes, attrs => attrs.DoesNotContainKey("missing"));
    }

    [Test]
    public async Task Member_Enumerable_IsInOrder_Passes()
    {
        var obj = new TestClass
        {
            Items = new List<int> { 1, 2, 3, 4, 5 }
        };

        await Assert.That(obj)
            .Member(x => x.Items, items => items.IsInOrder());
    }

    [Test]
    public async Task Member_Complex_Chain_Multiple_Collections()
    {
        var obj = new TestClass
        {
            Tags = ["important", "urgent"],
            Items = new List<int> { 1, 2, 3 },
            Attributes = new Dictionary<string, string> { ["status"] = "active" }
        };

        await Assert.That(obj)
            .IsNotNull()
            .And.Member(x => x.Tags, tags => tags.HasCount(2).And.Contains("important"))
            .And.Member(x => x.Items, items => items.HasCount(3).And.All(x => x > 0))
            .And.Member(x => x.Attributes, attrs => attrs.HasCount(1));
    }

    [Test]
    public async Task Member_Array_Contains_Predicate_Passes()
    {
        var obj = new TestClass
        {
            Tags = ["test1", "test2", "other"]
        };

        await Assert.That(obj)
            .Member(x => x.Tags, tags => tags.Contains(t => t.StartsWith("test")));
    }

    [Test]
    public async Task Member_Array_DoesNotContain_Passes()
    {
        var obj = new TestClass
        {
            Tags = ["tag1", "tag2"]
        };

        await Assert.That(obj)
            .Member(x => x.Tags, tags => tags.DoesNotContain("missing"));
    }

    [Test]
    public async Task Member_Array_HasSingleItem_Passes()
    {
        var obj = new TestClass
        {
            Tags = ["only"]
        };

        await Assert.That(obj)
            .Member(x => x.Tags, tags => tags.HasSingleItem());
    }

    [Test]
    public async Task Member_IEnumerable_HasCount_Passes()
    {
        var obj = new TestClass
        {
            Sequence = Enumerable.Range(1, 5)
        };

        await Assert.That(obj)
            .Member(x => x.Sequence, seq => seq.HasCount(5));
    }

    [Test]
    public async Task Issue3495_ReportedCase_Array_HasCount_And_Contains()
    {
        // This is the exact scenario reported in issue #3495
        var obj = new TestClass
        {
            Tags = ["pile", "other"]
        };

        await Assert.That(obj)
            .IsNotNull()
            .And.Member(x => x.Tags, tags => tags.HasCount(2).And.Contains("pile"));
    }

    [Test]
    public async Task Issue3495_ReportedCase_Dictionary_IsEmpty()
    {
        // This is the dictionary scenario mentioned in issue #3495
        var obj = new TestClass
        {
            Attributes = new Dictionary<string, string>()
        };

        await Assert.That(obj)
            .Member(x => x.Attributes, attrs => attrs.IsEmpty());
    }

    [Test]
    public async Task Issue3495_Dictionary_ContainsKey()
    {
        // Testing dictionary-specific methods like ContainsKey
        var obj = new TestClass
        {
            Attributes = new Dictionary<string, string> { ["status"] = "active" }
        };

        await Assert.That(obj)
            .Member(x => x.Attributes, attrs => attrs.ContainsKey("status").And.HasCount(1));
    }

    private class TestClass
    {
        public string[] Tags { get; init; } = [];
        public List<int> Items { get; init; } = [];
        public Dictionary<string, string> Attributes { get; init; } = new();
        public IEnumerable<int> Sequence { get; init; } = Enumerable.Empty<int>();
    }
}
