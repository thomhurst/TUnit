namespace TUnit.Engine.Services;

/// <summary>
/// Initializes static properties for tests based on the execution mode.
/// </summary>
internal interface IStaticPropertyInitializer
{
    /// <summary>
    /// Initializes all static properties.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken);
}
