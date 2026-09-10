using System.ComponentModel;
using OpenTelemetry.Trace;

namespace TUnit.OpenTelemetry;

/// <summary>
/// User-facing entry point for customizing the auto-wired <see cref="TracerProvider"/>
/// built by <see cref="AutoStart"/>. Callbacks are replayed on the shared builder after
/// TUnit adds its default sources and processors.
/// </summary>
public static class TUnitOpenTelemetry
{
    private static readonly List<Action<TracerProviderBuilder>> _configurators = [];
    private static readonly Lock _lock = new();

    /// <summary>
    /// Registers a callback to customize the auto-wired <see cref="TracerProviderBuilder"/>.
    /// Call from a static constructor or <c>[Before(HookType.TestDiscovery, Order = int.MinValue)]</c>
    /// hook so the callback runs before <see cref="AutoStart"/> builds the provider.
    /// </summary>
    public static void Configure(Action<TracerProviderBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        lock (_lock)
        {
            _configurators.Add(configure);
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static void ApplyConfiguration(TracerProviderBuilder builder)
    {
        Action<TracerProviderBuilder>[] snapshot;
        lock (_lock)
        {
            snapshot = [.. _configurators];
        }

        foreach (var configure in snapshot)
        {
            configure(builder);
        }
    }

    internal static bool HasConfiguration
    {
        get
        {
            lock (_lock)
            {
                return _configurators.Count > 0;
            }
        }
    }

    internal static void ResetForTests()
    {
        lock (_lock)
        {
            _configurators.Clear();
        }
    }
}
