namespace TUnit.TestProject.Bugs.Issue2993;

/// <summary>
/// Tests for issue #2993: Private types with implicit int? operator cause compilation failure
/// https://github.com/thomhurst/TUnit/issues/2993
/// </summary>
internal class ImplicitConversionTests
{
    // Test with nullable value type
    private record NullableIntRecord(int? Value)
    {
        public static implicit operator NullableIntRecord(int? value) => new(value);
        public static implicit operator int?(NullableIntRecord record) => record?.Value;
    }

    [Test]
    public async Task PrivateType_WithNullableIntImplicitOperator_ShouldCompile()
    {
        var items = Enumerable.Empty<NullableIntRecord>();
        await Assert.That(items).IsEmpty();
    }

    [Test]
    public async Task PrivateType_WithNullableIntImplicitOperator_NonEmptyCollection()
    {
        NullableIntRecord item1 = 42;
        NullableIntRecord item2 = null;
        var items = new[] { item1, item2 };

        await Assert.That(items).IsNotEmpty();
        await Assert.That(items).HasCount(2);
    }

    // Test with non-nullable value type
    private record IntRecord(int Value)
    {
        public static implicit operator IntRecord(int value) => new(value);
        public static implicit operator int(IntRecord record) => record.Value;
    }

    [Test]
    public async Task PrivateType_WithIntImplicitOperator_ShouldCompile()
    {
        var items = Enumerable.Empty<IntRecord>();
        await Assert.That(items).IsEmpty();
    }

    // Test with nullable reference type
    private record StringRecord(string? Value)
    {
        public static implicit operator StringRecord(string? value) => new(value);
        public static implicit operator string?(StringRecord record) => record?.Value;
    }

    [Test]
    public async Task PrivateType_WithNullableStringImplicitOperator_ShouldCompile()
    {
        var items = Enumerable.Empty<StringRecord>();
        await Assert.That(items).IsEmpty();
    }

    // Test with nested private type
    private class OuterClass
    {
        internal record InnerRecord(double? Value)
        {
            public static implicit operator InnerRecord(double? value) => new(value);
        }

        public static IEnumerable<InnerRecord> GetEmptyCollection() => Enumerable.Empty<InnerRecord>();
    }

    [Test]
    public async Task NestedPrivateType_WithImplicitOperator_ShouldCompile()
    {
        var items = OuterClass.GetEmptyCollection();
        await Assert.That(items).IsEmpty();
    }
}
