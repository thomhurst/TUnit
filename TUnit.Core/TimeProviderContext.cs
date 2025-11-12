using System.ComponentModel;

namespace TUnit.Core;

/// <summary>
/// Global TimeProvider configuration for TUnit test execution.
/// Uses AsyncLocal to ensure isolation between parallel tests.
/// For internal engine use. Set TimeProvider per test context for deterministic testing.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TimeProviderContext
{
    private static readonly AsyncLocal<TimeProvider?> _current = new();

    /// <summary>
    /// The TimeProvider used by the TUnit engine and all tests.
    /// Defaults to TimeProvider.System for real time.
    /// Uses AsyncLocal to ensure each test execution flow has isolated TimeProvider state.
    /// </summary>
    /// <example>
    /// <code>
    /// using System.Runtime.CompilerServices;
    /// using Microsoft.Extensions.Time.Testing;
    /// using TUnit.Core;
    ///
    /// [Test]
    /// public async Task MyTest()
    /// {
    ///     var fakeTimeProvider = new FakeTimeProvider();
    ///     TimeProviderContext.Current = fakeTimeProvider;
    ///
    ///     // Test code using TimeProviderContext.Current
    ///     // Each test gets its own isolated TimeProvider
    /// }
    /// </code>
    /// </example>
    public static TimeProvider Current
    {
        get => _current.Value ?? TimeProvider.System;
        set => _current.Value = value;
    }
}
