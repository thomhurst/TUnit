using TUnit.Mock.Arguments;

namespace TUnit.Mock.Tests;

/// <summary>
/// Interface for testing collection argument matchers.
/// </summary>
public interface ICollectionService
{
    int ProcessItems(List<int> items);
    string JoinNames(IEnumerable<string> names);
}

/// <summary>
/// US12 Tests: Collection argument matching (Contains, HasCount, IsEmpty, SequenceEquals).
/// </summary>
public class CollectionMatcherTests
{
    [Test]
    public async Task Arg_Contains_Matches_List_With_Item()
    {
        // Arrange
        var mock = Mock.Of<ICollectionService>();
        mock.Setup.ProcessItems(Arg.Contains<List<int>, int>(42)).Returns(1);

        // Act
        var svc = mock.Object;

        // Assert
        await Assert.That(svc.ProcessItems(new List<int> { 1, 42, 3 })).IsEqualTo(1);
        await Assert.That(svc.ProcessItems(new List<int> { 42 })).IsEqualTo(1);
    }

    [Test]
    public async Task Arg_Contains_Does_Not_Match_Without_Item()
    {
        // Arrange
        var mock = Mock.Of<ICollectionService>();
        mock.Setup.ProcessItems(Arg.Contains<List<int>, int>(42)).Returns(1);

        // Act
        var svc = mock.Object;

        // Assert — list without 42 returns default
        await Assert.That(svc.ProcessItems(new List<int> { 1, 2, 3 })).IsEqualTo(0);
        await Assert.That(svc.ProcessItems(new List<int>())).IsEqualTo(0);
    }

    [Test]
    public async Task Arg_HasCount_Matches_List_With_Exact_Count()
    {
        // Arrange
        var mock = Mock.Of<ICollectionService>();
        mock.Setup.ProcessItems(Arg.HasCount<List<int>>(3)).Returns(99);

        // Act
        var svc = mock.Object;

        // Assert
        await Assert.That(svc.ProcessItems(new List<int> { 1, 2, 3 })).IsEqualTo(99);
        await Assert.That(svc.ProcessItems(new List<int> { 10, 20, 30 })).IsEqualTo(99);
    }

    [Test]
    public async Task Arg_HasCount_Does_Not_Match_Wrong_Count()
    {
        // Arrange
        var mock = Mock.Of<ICollectionService>();
        mock.Setup.ProcessItems(Arg.HasCount<List<int>>(3)).Returns(99);

        // Act
        var svc = mock.Object;

        // Assert — wrong count returns default
        await Assert.That(svc.ProcessItems(new List<int> { 1, 2 })).IsEqualTo(0);
        await Assert.That(svc.ProcessItems(new List<int> { 1, 2, 3, 4 })).IsEqualTo(0);
    }

    [Test]
    public async Task Arg_IsEmpty_Matches_Empty_List()
    {
        // Arrange
        var mock = Mock.Of<ICollectionService>();
        mock.Setup.ProcessItems(Arg.IsEmpty<List<int>>()).Returns(77);

        // Act
        var svc = mock.Object;

        // Assert
        await Assert.That(svc.ProcessItems(new List<int>())).IsEqualTo(77);
    }

    [Test]
    public async Task Arg_IsEmpty_Does_Not_Match_NonEmpty()
    {
        // Arrange
        var mock = Mock.Of<ICollectionService>();
        mock.Setup.ProcessItems(Arg.IsEmpty<List<int>>()).Returns(77);

        // Act
        var svc = mock.Object;

        // Assert
        await Assert.That(svc.ProcessItems(new List<int> { 1 })).IsEqualTo(0);
    }

    [Test]
    public async Task Arg_SequenceEquals_Matches_Exact_Sequence()
    {
        // Arrange
        var mock = Mock.Of<ICollectionService>();
        mock.Setup.ProcessItems(Arg.SequenceEquals<List<int>, int>(new[] { 1, 2, 3 })).Returns(55);

        // Act
        var svc = mock.Object;

        // Assert
        await Assert.That(svc.ProcessItems(new List<int> { 1, 2, 3 })).IsEqualTo(55);
    }

    [Test]
    public async Task Arg_SequenceEquals_Does_Not_Match_Different_Sequence()
    {
        // Arrange
        var mock = Mock.Of<ICollectionService>();
        mock.Setup.ProcessItems(Arg.SequenceEquals<List<int>, int>(new[] { 1, 2, 3 })).Returns(55);

        // Act
        var svc = mock.Object;

        // Assert — wrong order or different elements
        await Assert.That(svc.ProcessItems(new List<int> { 3, 2, 1 })).IsEqualTo(0);
        await Assert.That(svc.ProcessItems(new List<int> { 1, 2 })).IsEqualTo(0);
        await Assert.That(svc.ProcessItems(new List<int> { 1, 2, 3, 4 })).IsEqualTo(0);
    }

    [Test]
    public async Task Arg_SequenceEquals_With_Strings()
    {
        // Arrange
        var mock = Mock.Of<ICollectionService>();
        mock.Setup.JoinNames(Arg.SequenceEquals<IEnumerable<string>, string>(new[] { "a", "b" })).Returns("matched");

        // Act
        var svc = mock.Object;

        // Assert
        await Assert.That(svc.JoinNames(new List<string> { "a", "b" })).IsEqualTo("matched");
        await Assert.That(svc.JoinNames(new[] { "a", "b" })).IsEqualTo("matched");
        await Assert.That(svc.JoinNames(new[] { "b", "a" })).IsEmpty();
    }

    [Test]
    public async Task Arg_Contains_With_Strings()
    {
        // Arrange
        var mock = Mock.Of<ICollectionService>();
        mock.Setup.JoinNames(Arg.Contains<IEnumerable<string>, string>("hello")).Returns("found");

        // Act
        var svc = mock.Object;

        // Assert
        await Assert.That(svc.JoinNames(new[] { "hello", "world" })).IsEqualTo("found");
        await Assert.That(svc.JoinNames(new[] { "goodbye" })).IsEmpty();
    }
}
