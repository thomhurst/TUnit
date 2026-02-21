using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

/// <summary>
/// Interface for auto-track property testing.
/// </summary>
public interface IAutoTrackEntity
{
    string Name { get; set; }
    int Age { get; set; }
    bool IsActive { get; set; }
    string ReadOnly { get; }
}

/// <summary>
/// US15 Tests: Auto-Track Properties (SetupAllProperties).
/// </summary>
public class AutoTrackPropertyTests
{
    [Test]
    public async Task AutoTrack_Set_Then_Get_Returns_Value()
    {
        // Arrange — opt in to auto-tracking
        var mock = Mock.Of<IAutoTrackEntity>();
        mock.SetupAllProperties();

        // Act
        mock.Object.Name = "Alice";
        var result = mock.Object.Name;

        // Assert
        await Assert.That(result).IsEqualTo("Alice");
    }

    [Test]
    public async Task AutoTrack_Multiple_Properties_Track_Independently()
    {
        // Arrange
        var mock = Mock.Of<IAutoTrackEntity>();
        mock.SetupAllProperties();

        // Act
        mock.Object.Name = "Bob";
        mock.Object.Age = 30;
        mock.Object.IsActive = true;

        // Assert
        await Assert.That(mock.Object.Name).IsEqualTo("Bob");
        await Assert.That(mock.Object.Age).IsEqualTo(30);
        await Assert.That(mock.Object.IsActive).IsTrue();
    }

    [Test]
    public async Task AutoTrack_Unset_Returns_Smart_Default()
    {
        // Arrange
        var mock = Mock.Of<IAutoTrackEntity>();

        // Assert — unset properties return smart defaults
        await Assert.That(mock.Object.Name).IsEmpty();
        await Assert.That(mock.Object.Age).IsEqualTo(0);
        await Assert.That(mock.Object.IsActive).IsFalse();
    }

    [Test]
    public async Task Explicit_Setup_Overrides_AutoTrack()
    {
        // Arrange
        var mock = Mock.Of<IAutoTrackEntity>();
        mock.SetupAllProperties();
        mock.Setup.Name_Get().Returns("Configured");

        // Act — set a tracked value, but explicit setup should win
        mock.Object.Name = "Tracked";

        // Assert — explicit setup takes precedence
        await Assert.That(mock.Object.Name).IsEqualTo("Configured");
    }

    [Test]
    public async Task AutoTrack_Overwrite_Value()
    {
        // Arrange
        var mock = Mock.Of<IAutoTrackEntity>();
        mock.SetupAllProperties();

        // Act — set then overwrite
        mock.Object.Name = "First";
        mock.Object.Name = "Second";

        // Assert
        await Assert.That(mock.Object.Name).IsEqualTo("Second");
    }

    [Test]
    public async Task Loose_Mode_Does_Not_AutoTrack_By_Default()
    {
        // Arrange — loose mode does NOT auto-track; requires explicit SetupAllProperties()
        var mock = Mock.Of<IAutoTrackEntity>();

        // Act
        mock.Object.Name = "Alice";

        // Assert — without SetupAllProperties(), getter returns smart default, not tracked value
        await Assert.That(mock.Object.Name).IsEmpty();
    }

    [Test]
    public async Task Loose_Mode_AutoTracks_After_SetupAllProperties()
    {
        // Arrange — explicit opt-in enables auto-tracking
        var mock = Mock.Of<IAutoTrackEntity>();
        mock.SetupAllProperties();

        // Act
        mock.Object.Name = "Alice";

        // Assert — auto-tracking is now on
        await Assert.That(mock.Object.Name).IsEqualTo("Alice");
    }

    [Test]
    public async Task Strict_Mode_Does_Not_AutoTrack_By_Default()
    {
        // Arrange — strict mode requires explicit SetupAllProperties
        var mock = Mock.Of<IAutoTrackEntity>(MockBehavior.Strict);
        mock.Setup.Name_Set(Arg.Any<string>());
        mock.Setup.Name_Get().Returns("");

        // Act
        mock.Object.Name = "Alice";

        // Assert — strict mode doesn't auto-track, returns setup value
        await Assert.That(mock.Object.Name).IsEmpty();
    }

    [Test]
    public async Task Strict_Mode_With_SetupAllProperties_Tracks()
    {
        // Arrange — strict mode with explicit opt-in
        var mock = Mock.Of<IAutoTrackEntity>(MockBehavior.Strict);
        mock.SetupAllProperties();
        mock.Setup.Name_Set(Arg.Any<string>());
        mock.Setup.Name_Get().Returns("");

        // Act
        mock.Object.Name = "Alice";

        // Assert — explicit setup still takes precedence over auto-track
        await Assert.That(mock.Object.Name).IsEmpty();
    }

    [Test]
    public async Task Reset_Clears_Tracked_Values_But_Keeps_AutoTrack()
    {
        // Arrange
        var mock = Mock.Of<IAutoTrackEntity>();
        mock.SetupAllProperties();
        mock.Object.Name = "Alice";

        // Act
        mock.Reset();

        // Assert — tracked values cleared, but auto-track still active
        await Assert.That(mock.Object.Name).IsEmpty();

        // Setting again works
        mock.Object.Name = "Bob";
        await Assert.That(mock.Object.Name).IsEqualTo("Bob");
    }

    [Test]
    public async Task ReadOnly_Property_Returns_Default()
    {
        // Arrange
        var mock = Mock.Of<IAutoTrackEntity>();

        // Assert — read-only property has no setter, returns smart default
        await Assert.That(mock.Object.ReadOnly).IsEmpty();
    }
}
