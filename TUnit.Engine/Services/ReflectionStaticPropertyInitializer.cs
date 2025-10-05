using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
using TUnit.Core.Services;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Services;

/// <summary>
/// Initializes static properties using both source-generated initializers and reflection-based discovery.
/// This implementation requires reflection and is NOT AOT-compatible.
/// </summary>
#if NET6_0_OR_GREATER
[RequiresUnreferencedCode("Uses reflection to discover and initialize static properties")]
[RequiresDynamicCode("Static property initialization may require dynamic code generation")]
#endif
internal sealed class ReflectionStaticPropertyInitializer : IStaticPropertyInitializer
{
    private readonly TUnitFrameworkLogger _logger;

    public ReflectionStaticPropertyInitializer(TUnitFrameworkLogger logger)
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

            // Additionally, initialize static properties discovered via reflection
            await StaticPropertyReflectionInitializer.InitializeAllStaticPropertiesAsync();
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync($"Error during static property initialization: {ex}");
            throw;
        }
    }
}
