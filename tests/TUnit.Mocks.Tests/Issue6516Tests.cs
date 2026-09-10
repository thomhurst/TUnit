using TUnit.Mocks;
using TUnit.Mocks.Arguments;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks.Tests;

// Regression: https://github.com/thomhurst/TUnit/issues/6516
// The indexer model never carried IsAbstractMember, so an abstract indexer on a mocked
// abstract class took the virtual-member path and emitted a `return base[...];` /
// `base[...] = value;` fallback — CS0205, there is no base implementation to call.
// Abstract indexers must dispatch through the engine only, exactly like abstract
// methods and abstract non-indexer properties already do.

#region Test types

// The issue's repro shape (mirrors DbDataReader's `this[int]` / `this[string]`).
public abstract class AbstractIndexerRepository
{
    public abstract object this[int index] { get; }
    public abstract object this[string key] { get; }
}

// Exercises the abstract setter branch too.
public abstract class AbstractReadWriteIndexer
{
    public abstract string this[int index] { get; set; }
}

// Virtual indexer must keep its base fallback.
public class VirtualIndexerStore
{
    public virtual string this[int index]
    {
        get => $"base-{index}";
        set { }
    }
}

#endregion

public class Issue6516Tests
{
    [Test]
    public async Task Abstract_GetOnly_Indexers_Compile_And_Return_Default_When_Unconfigured()
    {
        var mock = AbstractIndexerRepository.Mock();

        await Assert.That(mock.Object[0]).IsNull();
        await Assert.That(mock.Object["missing"]).IsNull();
    }

    [Test]
    public async Task Abstract_Indexer_Configured_Via_Setup_Returns_Value()
    {
        var mock = AbstractIndexerRepository.Mock();
        mock.Item(3).Returns("three");
        mock.Item("name").Returns("by-key");

        await Assert.That(mock.Object[3]).IsEqualTo("three");
        await Assert.That(mock.Object["name"]).IsEqualTo("by-key");

        mock.Item(3).WasCalled(Times.Once);
        mock.Item("name").WasCalled(Times.Once);
    }

    [Test]
    public async Task Abstract_ReadWrite_Indexer_Setter_Dispatches_Through_Engine()
    {
        var mock = AbstractReadWriteIndexer.Mock();
        mock.Item(1).Returns("one");

        await Assert.That(mock.Object[1]).IsEqualTo("one");

        mock.Object[7] = "seven";

        mock.SetItem(7, "seven").WasCalled(Times.Once);
        mock.SetItem(Any<int>(), Any<string>()).WasCalled(Times.Once);
    }

    [Test]
    public async Task Virtual_Indexer_Still_Falls_Back_To_Base_When_Unconfigured()
    {
        var mock = VirtualIndexerStore.Mock();
        mock.Item(1).Returns("configured");

        await Assert.That(mock.Object[1]).IsEqualTo("configured");
        await Assert.That(mock.Object[2]).IsEqualTo("base-2");
    }
}
