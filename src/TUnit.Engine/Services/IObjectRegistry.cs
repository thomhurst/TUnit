using System.Collections.Concurrent;
using System.Threading.Tasks;
using TUnit.Core;

namespace TUnit.Engine.Services;

/// <summary>
/// Interface for registering objects during test discovery and execution.
/// Used to break circular dependencies between property injection and object registration services.
/// </summary>
internal interface IObjectRegistry
{
    /// <summary>
    /// Registers a single object during the registration phase.
    /// Injects properties, tracks for disposal (once), but does NOT call IAsyncInitializer.
    /// </summary>
    /// <param name="instance">The object instance to register. Must not be null.</param>
    /// <param name="objectBag">Shared object bag for the test context. Must not be null.</param>
    /// <param name="methodMetadata">Method metadata for the test. Can be null.</param>
    /// <param name="events">Test context events for tracking. Must not be null and must be unique per test permutation.</param>
    Task RegisterObjectAsync(
        object instance,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers multiple argument objects during the registration phase.
    /// </summary>
    Task RegisterArgumentsAsync(
        object?[] arguments,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        CancellationToken cancellationToken = default);
}
