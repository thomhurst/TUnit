using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using TUnit.Core.Hooks;
using TUnit.Core.Interfaces.SourceGenerator;

namespace TUnit.Core;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
[StackTraceHidden]
/// <summary>
/// Provides methods to register various sources.
/// </summary>
public class SourceRegistrar
{
    private static readonly Lock ModeLock = new();
    private static volatile bool _isEnabled;
    private static volatile bool _isSourceGenerationAvailable;
    private static bool _isModeLocked;

    /// <summary>
    /// Gets whether the source-generated execution mode is in use.
    /// Generated module initializers set this to <c>true</c>. Once the engine has chosen the
    /// execution mode, later writes do not change it, so a module initializer from an assembly that
    /// loads mid-run cannot switch a reflection-mode run back to source-generated mode. Every write
    /// still updates <see cref="IsSourceGenerationAvailable"/>.
    /// </summary>
    /// <remarks>
    /// The engine owns this value after startup. Setting it once the engine has selected the
    /// execution mode has no effect. A write that would have changed the value is reported
    /// through <see cref="Trace.WriteLine(string)"/>. To choose the mode, use the
    /// <c>--reflection</c> or <c>--tunit-execution-mode</c> command-line options, the
    /// <c>TUNIT_EXECUTION_MODE</c> environment variable, or <see cref="ReflectionModeAttribute"/>.
    /// </remarks>
    public static bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            lock (ModeLock)
            {
                _isSourceGenerationAvailable = value;

                if (!_isModeLocked)
                {
                    _isEnabled = value;
                    return;
                }

                if (_isEnabled == value)
                {
                    return;
                }
            }

            ReportIgnoredWrite(value);
        }
    }

    /// <summary>
    /// Gets whether generated module initializers registered source-generated tests, independent of
    /// the execution mode the engine selected. Writes to <see cref="IsEnabled"/> update this value
    /// even after the mode is locked, so a later engine instance in the same process that has no
    /// explicit mode option still falls back to source-generated mode when it is available.
    /// </summary>
    internal static bool IsSourceGenerationAvailable => _isSourceGenerationAvailable;

    /// <summary>
    /// Sets the execution mode the engine selected and stops later <see cref="IsEnabled"/> writes
    /// from changing it. A later call, such as from another engine instance in the same process,
    /// replaces the locked value, so the most recent engine run decides the mode.
    /// </summary>
    internal static void LockExecutionMode(bool useSourceGeneration)
    {
        lock (ModeLock)
        {
            _isEnabled = useSourceGeneration;
            _isModeLocked = true;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ReportIgnoredWrite(bool value)
    {
        Trace.WriteLine(value
            ? "[TUnit.Core] Ignored SourceRegistrar.IsEnabled = true: the engine already selected reflection mode. A generated module initializer from an assembly that loaded after startup usually causes this."
            : "[TUnit.Core] Ignored SourceRegistrar.IsEnabled = false: the engine already selected source-generated mode. Use --reflection, --tunit-execution-mode, TUNIT_EXECUTION_MODE or [assembly: ReflectionMode] to choose the mode.");
    }

    /// <summary>
    /// Registers an assembly loader.
    /// </summary>
    /// <param name="assemblyLoader">The assembly loader to register.</param>
    public static void RegisterAssembly(Func<Assembly> assemblyLoader)
    {
#if NET
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            return;
        }
#endif

        Sources.AssemblyLoaders.Enqueue(assemblyLoader);
    }

    /// <summary>
    /// Registers a dynamic test source. Returns a dummy value so it can be used as a static field
    /// initializer on the consolidated registration <c>.cctor</c>, collapsing per-source module
    /// initializers into one merged <c>.cctor</c>.
    /// </summary>
    /// <param name="testSource">The test source to register.</param>
    public static int RegisterDynamic(IDynamicTestSource testSource)
    {
        Sources.DynamicTestSources.Enqueue(testSource);
        return 0;
    }

    /// <summary>
    /// Registers a global initializer.
    /// </summary>
    /// <param name="initializer">The initializer to register.</param>
    public static void RegisterGlobalInitializer(Func<Task> initializer)
    {
        Sources.GlobalInitializers.Enqueue(initializer);
    }

    /// <summary>
    /// Registers a property source (for property injection).
    /// </summary>
    /// <param name="propertySource">The property source to register.</param>
    public static void RegisterProperty(IPropertySource propertySource)
    {
        Sources.PropertySources.Enqueue(propertySource);
    }

    /// <summary>
    /// Registers a hook factory into a type-keyed dictionary. The factory is not invoked
    /// until the engine materializes the hook for execution. Use a <c>static</c> lambda
    /// (no captures) to keep module-init cost minimal and to remain AOT compatible.
    /// Returns a dummy value for use as a static field initializer.
    /// </summary>
    public static int RegisterHook<T>(ConcurrentDictionary<Type, ConcurrentBag<LazyHookEntry<T>>> dictionary, Type key, int registrationIndex, Func<int, T> factory)
        where T : HookMethod
    {
        dictionary.GetOrAdd(key, static _ => new ConcurrentBag<LazyHookEntry<T>>())
            .Add(new LazyHookEntry<T>(registrationIndex, factory));
        return 0;
    }

    /// <summary>
    /// Registers a hook factory into an assembly-keyed dictionary. The factory is not invoked
    /// until the engine materializes the hook for execution. Use a <c>static</c> lambda
    /// (no captures) to keep module-init cost minimal and to remain AOT compatible.
    /// Returns a dummy value for use as a static field initializer.
    /// </summary>
    public static int RegisterHook<T>(ConcurrentDictionary<Assembly, ConcurrentBag<LazyHookEntry<T>>> dictionary, Assembly key, int registrationIndex, Func<int, T> factory)
        where T : HookMethod
    {
        dictionary.GetOrAdd(key, static _ => new ConcurrentBag<LazyHookEntry<T>>())
            .Add(new LazyHookEntry<T>(registrationIndex, factory));
        return 0;
    }

    /// <summary>
    /// Registers a hook factory into a global bag. The factory is not invoked until the engine
    /// materializes the hook for execution. Use a <c>static</c> lambda (no captures) to keep
    /// module-init cost minimal and to remain AOT compatible.
    /// Returns a dummy value for use as a static field initializer.
    /// </summary>
    public static int RegisterHook<T>(ConcurrentBag<LazyHookEntry<T>> bag, int registrationIndex, Func<int, T> factory)
        where T : HookMethod
    {
        bag.Add(new LazyHookEntry<T>(registrationIndex, factory));
        return 0;
    }

    /// <summary>
    /// Registers a factory for test entries. The factory is not invoked until the engine
    /// needs to access the entries (during discovery/filtering), avoiding per-class JIT
    /// compilation during module initialization.
    /// Returns a dummy value for use as a static field initializer.
    /// Multiple calls for the same T are additive — factories accumulate.
    /// </summary>
    public static int RegisterEntries<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)] T>(Func<TestEntry<T>[]> factory) where T : class
    {
        var key = typeof(T);

        while (true)
        {
            if (Sources.TestEntries.TryGetValue(key, out var existing))
            {
                if (existing is TestEntrySource<T> existingSource)
                {
                    existingSource.AddFactory(factory);
                    return 0;
                }

                throw new InvalidOperationException(
                    $"Type mismatch in TestEntries for '{typeof(T).FullName}': expected TestEntrySource<{typeof(T).Name}>, found {existing.GetType().Name}");
            }

            if (Sources.TestEntries.TryAdd(key, new TestEntrySource<T>(factory)))
            {
                return 0;
            }

            // Another thread added between TryGetValue and TryAdd — retry to merge
        }
    }

    /// <summary>
    /// Registers test methods of the open generic type <paramref name="genericTypeDefinition"/> whose
    /// inferred class type arguments break the class's generic constraints. Each message is reported
    /// as a failed test. Returns a dummy value for use as a static field initializer.
    /// Multiple calls for the same type are additive.
    /// </summary>
    public static int RegisterGenericConstraintViolations([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)] Type genericTypeDefinition, Func<GenericConstraintViolationEntry[]> factory)
    {
        while (true)
        {
            if (Sources.TestEntries.TryGetValue(genericTypeDefinition, out var existing))
            {
                if (existing is GenericConstraintViolationEntrySource existingSource)
                {
                    existingSource.AddFactory(factory);
                    return 0;
                }

                throw new InvalidOperationException(
                    $"Type mismatch in TestEntries for '{genericTypeDefinition.FullName}': expected {nameof(GenericConstraintViolationEntrySource)}, found {existing.GetType().Name}");
            }

            if (Sources.TestEntries.TryAdd(genericTypeDefinition, new GenericConstraintViolationEntrySource(genericTypeDefinition, factory)))
            {
                return 0;
            }
        }
    }
}
