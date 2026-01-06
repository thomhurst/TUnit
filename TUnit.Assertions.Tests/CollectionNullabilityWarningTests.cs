namespace TUnit.Assertions.Tests;

/// <summary>
/// Tests to ensure that Assert.That() accepts nullable collection types without generating
/// nullability warnings (CS8604, CS8625, etc.). This validates the fix for GitHub issue reporting
/// false positive nullability warnings for List, IList, Dictionary, IDictionary types.
/// </summary>
public class CollectionNullabilityWarningTests
{
    // ===================================
    // List<T>? Tests
    // ===================================

    [Test]
    public async Task NullableList_AcceptsNullableValue_NoWarning()
    {
        List<string>? list = ["a", "b", "c"];
        await Assert.That(list).IsNotNull();
    }

    [Test]
    public async Task NullableList_WithNullValue_IsNotNull_Fails()
    {
        List<string>? list = null;
        var action = async () => await Assert.That(list).IsNotNull();
        await Assert.That(action).Throws<AssertionException>();
    }

    [Test]
    public async Task NullableList_WithNullValue_IsNull_Passes()
    {
        List<string>? list = null;
        await Assert.That(list).IsNull();
    }

    // ===================================
    // IList<T>? Tests
    // ===================================

    [Test]
    public async Task NullableIList_AcceptsNullableValue_NoWarning()
    {
        IList<string>? list = new List<string> { "a", "b", "c" };
        await Assert.That(list).IsNotNull();
    }

    [Test]
    public async Task NullableIList_WithNullValue_IsNotNull_Fails()
    {
        IList<string>? list = null;
        var action = async () => await Assert.That(list).IsNotNull();
        await Assert.That(action).Throws<AssertionException>();
    }

    [Test]
    public async Task NullableIList_WithNullValue_IsNull_Passes()
    {
        IList<string>? list = null;
        await Assert.That(list).IsNull();
    }

    // ===================================
    // Dictionary<TKey, TValue>? Tests
    // ===================================

    [Test]
    public async Task NullableDictionary_AcceptsNullableValue_NoWarning()
    {
        Dictionary<string, int>? dict = new() { ["key"] = 1 };
        await Assert.That(dict).IsNotNull();
    }

    [Test]
    public async Task NullableDictionary_WithNullValue_IsNotNull_Fails()
    {
        Dictionary<string, int>? dict = null;
        var action = async () => await Assert.That(dict).IsNotNull();
        await Assert.That(action).Throws<AssertionException>();
    }

    [Test]
    public async Task NullableDictionary_WithNullValue_IsNull_Passes()
    {
        Dictionary<string, int>? dict = null;
        await Assert.That(dict).IsNull();
    }

    // ===================================
    // IDictionary<TKey, TValue>? Tests
    // ===================================

    [Test]
    public async Task NullableIDictionary_AcceptsNullableValue_NoWarning()
    {
        IDictionary<string, int>? dict = new Dictionary<string, int> { ["key"] = 1 };
        await Assert.That(dict).IsNotNull();
    }

    [Test]
    public async Task NullableIDictionary_WithNullValue_IsNotNull_Fails()
    {
        IDictionary<string, int>? dict = null;
        var action = async () => await Assert.That(dict).IsNotNull();
        await Assert.That(action).Throws<AssertionException>();
    }

    [Test]
    public async Task NullableIDictionary_WithNullValue_IsNull_Passes()
    {
        IDictionary<string, int>? dict = null;
        await Assert.That(dict).IsNull();
    }

    // ===================================
    // IReadOnlyDictionary<TKey, TValue>? Tests
    // ===================================

    [Test]
    public async Task NullableIReadOnlyDictionary_AcceptsNullableValue_NoWarning()
    {
        IReadOnlyDictionary<string, int>? dict = new Dictionary<string, int> { ["key"] = 1 };
        await Assert.That(dict).IsNotNull();
    }

    [Test]
    public async Task NullableIReadOnlyDictionary_WithNullValue_IsNotNull_Fails()
    {
        IReadOnlyDictionary<string, int>? dict = null;
        var action = async () => await Assert.That(dict).IsNotNull();
        await Assert.That(action).Throws<AssertionException>();
    }

    [Test]
    public async Task NullableIReadOnlyDictionary_WithNullValue_IsNull_Passes()
    {
        IReadOnlyDictionary<string, int>? dict = null;
        await Assert.That(dict).IsNull();
    }

    // ===================================
    // Array? Tests
    // ===================================

    [Test]
    public async Task NullableArray_AcceptsNullableValue_NoWarning()
    {
        string[]? arr = ["a", "b", "c"];
        await Assert.That(arr).IsNotNull();
    }

    [Test]
    public async Task NullableArray_WithNullValue_IsNotNull_Fails()
    {
        string[]? arr = null;
        var action = async () => await Assert.That(arr).IsNotNull();
        await Assert.That(action).Throws<AssertionException>();
    }

    [Test]
    public async Task NullableArray_WithNullValue_IsNull_Passes()
    {
        string[]? arr = null;
        await Assert.That(arr).IsNull();
    }

    // ===================================
    // ISet<T>? Tests
    // ===================================

    [Test]
    public async Task NullableISet_AcceptsNullableValue_NoWarning()
    {
        ISet<string>? set = new HashSet<string> { "a", "b", "c" };
        await Assert.That(set).IsNotNull();
    }

    [Test]
    public async Task NullableISet_WithNullValue_IsNotNull_Fails()
    {
        ISet<string>? set = null;
        var action = async () => await Assert.That(set).IsNotNull();
        await Assert.That(action).Throws<AssertionException>();
    }

    [Test]
    public async Task NullableISet_WithNullValue_IsNull_Passes()
    {
        ISet<string>? set = null;
        await Assert.That(set).IsNull();
    }

    // ===================================
    // HashSet<T>? Tests
    // ===================================

    [Test]
    public async Task NullableHashSet_AcceptsNullableValue_NoWarning()
    {
        HashSet<string>? set = ["a", "b", "c"];
        await Assert.That(set).IsNotNull();
    }

    [Test]
    public async Task NullableHashSet_WithNullValue_IsNotNull_Fails()
    {
        HashSet<string>? set = null;
        var action = async () => await Assert.That(set).IsNotNull();
        await Assert.That(action).Throws<AssertionException>();
    }

    [Test]
    public async Task NullableHashSet_WithNullValue_IsNull_Passes()
    {
        HashSet<string>? set = null;
        await Assert.That(set).IsNull();
    }

    // ===================================
    // IReadOnlyList<T>? Tests
    // ===================================

    [Test]
    public async Task NullableIReadOnlyList_AcceptsNullableValue_NoWarning()
    {
        IReadOnlyList<string>? list = new List<string> { "a", "b", "c" };
        await Assert.That(list).IsNotNull();
    }

    [Test]
    public async Task NullableIReadOnlyList_WithNullValue_IsNotNull_Fails()
    {
        IReadOnlyList<string>? list = null;
        var action = async () => await Assert.That(list).IsNotNull();
        await Assert.That(action).Throws<AssertionException>();
    }

    [Test]
    public async Task NullableIReadOnlyList_WithNullValue_IsNull_Passes()
    {
        IReadOnlyList<string>? list = null;
        await Assert.That(list).IsNull();
    }

    // ===================================
    // Method parameter tests (original issue scenario)
    // ===================================

    [Test]
    public async Task MethodWithNullableListParameter_NoWarning()
    {
        List<string>? list = ["test"];
        await VerifyNotNull(list);
    }

    [Test]
    public async Task MethodWithNullableIDictionaryParameter_NoWarning()
    {
        IDictionary<string, int>? dict = new Dictionary<string, int> { ["key"] = 1 };
        await VerifyDictionaryNotNull(dict);
    }

    private static async Task VerifyNotNull(List<string>? actual)
    {
        await Assert.That(actual).IsNotNull();
    }

    private static async Task VerifyDictionaryNotNull(IDictionary<string, int>? actual)
    {
        await Assert.That(actual).IsNotNull();
    }
}
