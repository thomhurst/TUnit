using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

/// <summary>
/// Interfaces that inherit from IEnumerable&lt;T&gt; — verifies the explicit
/// IEnumerable.GetEnumerator() implementation is generated correctly (#5559).
/// </summary>
public interface ITestEnum<T> : IEnumerable<T>
{
}

public interface ICustomCollection : IEnumerable<int>
{
    int ItemCount { get; }
    void Add(int item);
}

public interface IPagedResult<T> : IEnumerable<T>
{
    int TotalCount { get; }
    int PageSize { get; }
}

public interface ITestContainer
{
    ITestEnum<string> Items { get; }
}

public class EnumerableInterfaceTests
{
    [Test]
    public async Task Mock_Of_IEnumerable_Interface_Creates_Instance()
    {
        // Arrange & Act
        var mock = ITestEnum<string>.Mock();

        // Assert
        await Assert.That(mock).IsNotNull();
        await Assert.That(mock.Object).IsNotNull();
    }

    [Test]
    public async Task Mock_IEnumerable_Interface_Can_Configure_GetEnumerator()
    {
        // Arrange
        var items = new List<string> { "a", "b", "c" };
        var mock = ITestEnum<string>.Mock();
        mock.GetEnumerator().Returns(items.GetEnumerator());

        // Act
        ITestEnum<string> obj = mock.Object;
        var result = new List<string>();
        using var enumerator = obj.GetEnumerator();
        while (enumerator.MoveNext())
        {
            result.Add(enumerator.Current);
        }

        // Assert
        await Assert.That(result).IsEquivalentTo(items);
    }

    [Test]
    public async Task Mock_IEnumerable_Interface_Works_With_Foreach()
    {
        // Arrange
        var items = new List<string> { "x", "y" };
        var mock = ITestEnum<string>.Mock();
        mock.GetEnumerator().Returns(items.GetEnumerator());

        // Act — foreach uses IEnumerable<T>.GetEnumerator()
        var result = new List<string>();
        foreach (var item in mock.Object)
        {
            result.Add(item);
        }

        // Assert
        await Assert.That(result).IsEquivalentTo(items);
    }

    [Test]
    public async Task Mock_IEnumerable_Interface_Supports_Cast_To_Non_Generic_IEnumerable()
    {
        // Arrange
        var items = new List<string> { "hello" };
        var mock = ITestEnum<string>.Mock();
        mock.GetEnumerator().Returns(items.GetEnumerator());

        // Act — cast to non-generic IEnumerable and iterate
        IEnumerable nonGeneric = mock.Object;
        var result = new List<object?>();
        foreach (var item in nonGeneric)
        {
            result.Add(item);
        }

        // Assert
        await Assert.That(result).Count().IsEqualTo(1);
    }

    [Test]
    public async Task Mock_ICustomCollection_With_IEnumerable_And_Extra_Members()
    {
        // Arrange
        var mock = ICustomCollection.Mock();
        mock.ItemCount.Returns(3);

        var items = new List<int> { 10, 20, 30 };
        mock.GetEnumerator().Returns(items.GetEnumerator());

        // Act
        ICustomCollection col = mock.Object;
        var mockCount = col.ItemCount;
        var result = new List<int>();
        using var enumerator = col.GetEnumerator();
        while (enumerator.MoveNext())
        {
            result.Add(enumerator.Current);
        }

        // Assert
        await Assert.That(mockCount).IsEqualTo(3);
        await Assert.That(result).IsEquivalentTo(items);
    }

    [Test]
    public async Task Mock_IPagedResult_Supports_Properties_And_Enumeration()
    {
        // Arrange
        var mock = IPagedResult<string>.Mock();
        mock.TotalCount.Returns(100);
        mock.PageSize.Returns(10);

        var page = new List<string> { "item1", "item2" };
        mock.GetEnumerator().Returns(page.GetEnumerator());

        // Act
        IPagedResult<string> paged = mock.Object;

        // Assert
        await Assert.That(paged.TotalCount).IsEqualTo(100);
        await Assert.That(paged.PageSize).IsEqualTo(10);

        var result = new List<string>();
        foreach (var item in paged)
        {
            result.Add(item);
        }

        await Assert.That(result).IsEquivalentTo(page);
    }

    [Test]
    public async Task Mock_IEnumerable_Interface_Non_Generic_GetEnumerator_Delegates_To_Generic()
    {
        // Arrange — configure only the generic GetEnumerator
        var items = new List<int> { 1, 2, 3 };
        var mock = ICustomCollection.Mock();
        mock.GetEnumerator().Returns(items.GetEnumerator());

        // Act — call non-generic IEnumerable.GetEnumerator() explicitly
        IEnumerable nonGeneric = mock.Object;
        var enumerator = nonGeneric.GetEnumerator();
        var result = new List<object?>();
        while (enumerator.MoveNext())
        {
            result.Add(enumerator.Current);
        }

        // Assert — non-generic delegates to generic, so same items
        await Assert.That(result).Count().IsEqualTo(3);
    }

    [Test]
    public async Task Mock_IEnumerable_Interface_Verify_GetEnumerator_Called()
    {
        // Arrange
        var mock = ITestEnum<string>.Mock();
        mock.GetEnumerator().Returns(new List<string>().GetEnumerator());

        // Act
        using var _ = mock.Object.GetEnumerator();

        // Assert
        mock.GetEnumerator().WasCalled(Times.Once);
    }
}
