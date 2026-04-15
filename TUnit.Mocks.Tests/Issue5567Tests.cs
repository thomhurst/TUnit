using System.Collections.Generic;
using TUnit.Mocks;

namespace TUnit.Mocks.Tests;

public interface IIssue5567Root
{
    IIssue5567Enumerable<string> Items { get; }
    IIssue5567Enumerable<T> Create<T>();
}

public interface IIssue5567Enumerable<T> : IEnumerable<T>
{
    T? GetValue();
}

public class Issue5567Tests
{
    [Test]
    public async Task Auto_Mock_Closed_Generic_IEnumerable_Return_Is_Functional()
    {
        var mock = IIssue5567Root.Mock();

        var items = mock.Object.Items;
        await Assert.That(items).IsNotNull();

        var itemsMock = Mock.Get(items);
        itemsMock.GetValue().Returns("hello");

        await Assert.That(items.GetValue()).IsEqualTo("hello");
    }

    [Test]
    public async Task Auto_Mock_Open_Generic_Method_Return_Is_Functional()
    {
        var mock = IIssue5567Root.Mock();

        var created = mock.Object.Create<int>();
        await Assert.That(created).IsNotNull();

        var createdMock = Mock.Get(created);
        createdMock.GetValue().Returns(42);

        await Assert.That(created.GetValue()).IsEqualTo(42);
    }
}
