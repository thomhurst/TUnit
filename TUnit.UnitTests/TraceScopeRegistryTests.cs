#if NET
using System.Runtime.CompilerServices;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.UnitTests;

public class TraceScopeRegistryTests
{
    [Test]
    public async Task RegisterFromDataSource_WithTraceScopeProvider_RegistersSharedTypes()
    {
        var obj1 = new object();
        var obj2 = new object();
        var dataSource = new FakeTraceScopeDataSource(
            [SharedType.PerAssembly, SharedType.PerClass]);

        TraceScopeRegistry.RegisterFromDataSource(dataSource, [obj1, obj2]);

        await Assert.That(TraceScopeRegistry.GetSharedType(obj1)).IsEqualTo(SharedType.PerAssembly);
        await Assert.That(TraceScopeRegistry.GetSharedType(obj2)).IsEqualTo(SharedType.PerClass);
    }

    [Test]
    public async Task RegisterFromDataSource_WithNonTraceScopeProvider_DoesNotRegister()
    {
        var obj = new object();
        var dataSource = new FakeNonTraceScopeDataSource();

        TraceScopeRegistry.RegisterFromDataSource(dataSource, [obj]);

        await Assert.That(TraceScopeRegistry.GetSharedType(obj)).IsNull();
    }

    [Test]
    public async Task GetSharedType_UnregisteredObject_ReturnsNull()
    {
        var unregistered = new object();

        await Assert.That(TraceScopeRegistry.GetSharedType(unregistered)).IsNull();
    }

    [Test]
    public async Task RegisterFromDataSource_WithNullObjectsArray_DoesNotThrow()
    {
        var dataSource = new FakeTraceScopeDataSource([SharedType.None]);

        // Should not throw — just a no-op
        TraceScopeRegistry.RegisterFromDataSource(dataSource, null);

        // Verify no side effects — a new object should still be unregistered
        var probe = new object();
        await Assert.That(TraceScopeRegistry.GetSharedType(probe)).IsNull();
    }

    [Test]
    public async Task RegisterFromDataSource_WithEmptyObjectsArray_DoesNotThrow()
    {
        var dataSource = new FakeTraceScopeDataSource([SharedType.None]);

        TraceScopeRegistry.RegisterFromDataSource(dataSource, []);

        // Verify no side effects
        var probe = new object();
        await Assert.That(TraceScopeRegistry.GetSharedType(probe)).IsNull();
    }

    [Test]
    public async Task RegisterFromDataSource_WithNullElementsInArray_SkipsNulls()
    {
        var realObj = new object();
        var dataSource = new FakeTraceScopeDataSource(
            [SharedType.PerTestSession, SharedType.PerAssembly]);

        TraceScopeRegistry.RegisterFromDataSource(dataSource, [null, realObj]);

        // null element is skipped, realObj gets the second SharedType
        await Assert.That(TraceScopeRegistry.GetSharedType(realObj)).IsEqualTo(SharedType.PerAssembly);
    }

    [Test]
    public async Task RegisterFromDataSource_DuplicateObject_KeepsFirstRegistration()
    {
        var obj = new object();
        var firstSource = new FakeTraceScopeDataSource([SharedType.PerTestSession]);
        var secondSource = new FakeTraceScopeDataSource([SharedType.PerClass]);

        TraceScopeRegistry.RegisterFromDataSource(firstSource, [obj]);
        TraceScopeRegistry.RegisterFromDataSource(secondSource, [obj]);

        // First registration wins
        await Assert.That(TraceScopeRegistry.GetSharedType(obj)).IsEqualTo(SharedType.PerTestSession);
    }

    [Test]
    public async Task RegisterFromDataSource_FewerSharedTypesThanObjects_DefaultsToNone()
    {
        var obj1 = new object();
        var obj2 = new object();
        // Only one SharedType for two objects
        var dataSource = new FakeTraceScopeDataSource([SharedType.PerAssembly]);

        TraceScopeRegistry.RegisterFromDataSource(dataSource, [obj1, obj2]);

        await Assert.That(TraceScopeRegistry.GetSharedType(obj1)).IsEqualTo(SharedType.PerAssembly);
        await Assert.That(TraceScopeRegistry.GetSharedType(obj2)).IsEqualTo(SharedType.None);
    }

    [Test]
    public async Task RegisterFromDataSource_UsesReferenceEquality()
    {
        var obj1 = new EquatableObject(42);
        var obj2 = new EquatableObject(42); // Same value but different reference
        var dataSource = new FakeTraceScopeDataSource(
            [SharedType.PerTestSession, SharedType.PerClass]);

        TraceScopeRegistry.RegisterFromDataSource(dataSource, [obj1, obj2]);

        // Should track separately despite Equals returning true
        await Assert.That(TraceScopeRegistry.GetSharedType(obj1)).IsEqualTo(SharedType.PerTestSession);
        await Assert.That(TraceScopeRegistry.GetSharedType(obj2)).IsEqualTo(SharedType.PerClass);
    }

    /// <summary>
    /// Fake data source that implements both IDataSourceAttribute and ITraceScopeProvider.
    /// </summary>
    private sealed class FakeTraceScopeDataSource : IDataSourceAttribute, ITraceScopeProvider
    {
        private readonly SharedType[] _sharedTypes;

        public FakeTraceScopeDataSource(SharedType[] sharedTypes)
        {
            _sharedTypes = sharedTypes;
        }

        public bool SkipIfEmpty { get; set; }

        public IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
            => throw new NotSupportedException("Not needed for registry tests");

        public IEnumerable<SharedType> GetSharedTypes() => _sharedTypes;
    }

    /// <summary>
    /// Fake data source that does NOT implement ITraceScopeProvider.
    /// </summary>
    private sealed class FakeNonTraceScopeDataSource : IDataSourceAttribute
    {
        public bool SkipIfEmpty { get; set; }

        public IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
            => throw new NotSupportedException("Not needed for registry tests");
    }

    /// <summary>
    /// Object with value-based equality to test that the registry uses reference equality.
    /// </summary>
    private sealed class EquatableObject : IEquatable<EquatableObject>
    {
        private readonly int _value;

        public EquatableObject(int value) => _value = value;

        public bool Equals(EquatableObject? other) => other is not null && _value == other._value;
        public override bool Equals(object? obj) => Equals(obj as EquatableObject);
        public override int GetHashCode() => _value;
    }
}
#endif
