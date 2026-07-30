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

    /// <summary>
    /// When a loose-mode mock must return an interface the source generator produced no mock
    /// for — most notably a generic method invoked by third-party code with a type argument
    /// that is <c>internal</c> to another assembly — emit a functional stub at runtime instead
    /// of returning <see langword="null"/>. Matches the auto-substitution behavior of
    /// runtime-proxy mocking libraries. Defaults to <see langword="true"/>; has no effect on
    /// Native AOT, where the previous default-value behavior is retained.
    /// </summary>
    public bool RuntimeAutoStubs { get; set; } = true;
}
