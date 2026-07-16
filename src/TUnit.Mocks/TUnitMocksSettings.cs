using TUnit.Core.Settings;

namespace TUnit.Mocks;

public static class TUnitMocksSettingsExtensions
{
    extension(TUnitSettings _)
    {
        // TUnit.Mocks cannot add instance state to TUnitSettings, so package settings live in this singleton.
        public TUnitMocksSettings Mocks => TUnitMocksSettings.Default;
    }
}

public class TUnitMocksSettings
{
    internal static readonly TUnitMocksSettings Default = new();

    internal TUnitMocksSettings()
    {
    }

    /// <summary>
    /// Default behavior used when creating mocks without an explicit <see cref="MockBehavior"/>.
    /// </summary>
    /// <remarks>
    /// Configure this during test discovery, before tests create mocks.
    /// </remarks>
    public MockBehavior DefaultMode { get; set; } = MockBehavior.Loose;
}
