using System.ComponentModel;

namespace TUnit.Core;

/// <summary>
/// Global TimeProvider configuration for TUnit test execution.
/// For internal engine use. Set TimeProvider in a [ModuleInitializer] to use FakeTimeProvider for deterministic testing.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TimeProviderContext
{
    /// <summary>
    /// The TimeProvider used by the TUnit engine and all tests.
    /// Defaults to TimeProvider.System for real time.
    /// Set to FakeTimeProvider in a [ModuleInitializer] for deterministic test timing.
    /// </summary>
    /// <example>
    /// <code>
    /// using System.Runtime.CompilerServices;
    /// using Microsoft.Extensions.Time.Testing;
    /// using TUnit.Core;
    ///
    /// public static class TestSetup
    /// {
    ///     [ModuleInitializer]
    ///     public static void Initialize()
    ///     {
    ///         TimeProviderContext.Current = new FakeTimeProvider();
    ///     }
    /// }
    /// </code>
    /// </example>
    public static TimeProvider Current { get; set; } = TimeProvider.System;
}
