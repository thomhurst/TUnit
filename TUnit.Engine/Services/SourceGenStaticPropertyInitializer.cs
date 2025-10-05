using TUnit.Core;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Services;

/// <summary>
/// Initializes static properties using source-generated initializers only.
/// This implementation does not use reflection and is AOT-compatible.
/// </summary>
internal sealed class SourceGenStaticPropertyInitializer : IStaticPropertyInitializer
{
    private readonly TUnitFrameworkLogger _logger;

    public SourceGenStaticPropertyInitializer(TUnitFrameworkLogger logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Execute all registered global initializers from source generation
            while (Sources.GlobalInitializers.TryDequeue(out var initializer))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await initializer();
            }
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync($"Error during static property initialization: {ex}");
            throw;
        }
    }
}
