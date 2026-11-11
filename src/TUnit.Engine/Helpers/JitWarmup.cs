#if NET
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Runtime.CompilerServices;
using TUnit.Core;
using TUnit.Engine.Configuration;

namespace TUnit.Engine.Helpers;

/// <summary>
/// Compiles the engine's startup methods on a background thread while Microsoft.Testing.Platform
/// is still bootstrapping on the main thread.
/// </summary>
/// <remarks>
/// A short test run is dominated by JIT time, and the JIT compiles each method on the thread that
/// first calls it. The platform spends its bootstrap on one thread before any engine code runs,
/// so the engine's methods can be compiled on otherwise idle cores in that window.
/// <para>
/// The methods come from <c>JitWarmupMethods.txt</c>, an embedded list in first-use order. Each
/// line is <c>TypeFullName::SourceMethodName</c>; the lambdas, local functions and state machines
/// the compiler generates for that method are warmed with it, so the list does not depend on
/// compiler-generated names. Entries that no longer resolve are skipped. To regenerate the list,
/// run a representative test app with <c>TUNIT_JIT_WARMUP_RECORD</c> set to an output path.
/// </para>
/// </remarks>
internal static class JitWarmup
{
    private const string ResourceName = "TUnit.Engine.JitWarmupMethods.txt";
    private const string Separator = "::";

    private const BindingFlags DeclaredMembers =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static int s_started;

    public static void Start()
    {
        // Native AOT has nothing to compile, and the browser has no threads to compile on.
        if (!RuntimeFeature.IsDynamicCodeCompiled || OperatingSystem.IsBrowser() || OperatingSystem.IsWasi())
        {
            return;
        }

        if (Interlocked.Exchange(ref s_started, 1) != 0)
        {
            return;
        }

        if (Environment.GetEnvironmentVariable(EnvironmentConstants.JitWarmupRecord) is { Length: > 0 } recordPath)
        {
            // Warming while recording would record the list itself rather than what the run used.
            Recorder.Start(recordPath);
            return;
        }

        // With one core the warm-up would only compete with the main thread.
        if (EnvironmentFlags.IsSet(EnvironmentConstants.DisableJitWarmup) || Environment.ProcessorCount < 2)
        {
            return;
        }

        // One thread is enough: it finishes the list before the platform hands over to the engine.
        new Thread(static () =>
        {
            try
            {
                WarmEntries();
            }
            catch
            {
                // Best effort: a failed warm-up only means those methods compile on first use.
            }
        })
        {
            IsBackground = true,
            Name = "TUnit JIT warm-up",
        }.Start();
    }

    private static void WarmEntries()
    {
        var membersByType = new Dictionary<Type, TypeMembers>();

        foreach (var entry in ReadEntries())
        {
            foreach (var method in ResolveEntry(entry, membersByType))
            {
                try
                {
                    RuntimeHelpers.PrepareMethod(method.MethodHandle);
                }
                catch
                {
                    // Methods without a compilable body are not worth failing the warm-up over.
                }
            }
        }
    }

    internal static IEnumerable<string> ReadEntries()
    {
        using var stream = typeof(JitWarmup).Assembly.GetManifestResourceStream(ResourceName);

        if (stream is null)
        {
            yield break;
        }

        using var reader = new StreamReader(stream);

        while (reader.ReadLine() is { } entry)
        {
            yield return entry;
        }
    }

    /// <summary>The methods an entry warms; empty when the entry no longer matches the code.</summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Best effort: a type or member the trimmer removed is not warmed.")]
    internal static IEnumerable<MethodBase> ResolveEntry(string entry, Dictionary<Type, TypeMembers> membersByType)
    {
        var separator = entry.IndexOf(Separator, StringComparison.Ordinal);

        if (separator < 0)
        {
            return [];
        }

        var typeName = entry[..separator];
        var type = typeof(JitWarmup).Assembly.GetType(typeName) ?? typeof(TestContext).Assembly.GetType(typeName);

        if (type is null || type.ContainsGenericParameters)
        {
            return [];
        }

        if (!membersByType.TryGetValue(type, out var members))
        {
            membersByType[type] = members = new TypeMembers(type);
        }

        return FindMethods(members, entry[(separator + Separator.Length)..]);
    }

    /// <summary>
    /// The method named <paramref name="methodName"/> and everything the compiler generated for it.
    /// </summary>
    internal static IEnumerable<MethodBase> FindMethods(TypeMembers members, string methodName)
    {
        foreach (var method in members.Own)
        {
            if (method.Name == methodName || IsGeneratedFor(method.Name, methodName))
            {
                yield return method;
            }
        }

        foreach (var (nestedName, nestedMethods) in members.Generated)
        {
            var isStateMachine = IsGeneratedFor(nestedName, methodName);

            foreach (var method in nestedMethods)
            {
                if (isStateMachine || IsGeneratedFor(method.Name, methodName))
                {
                    yield return method;
                }
            }
        }
    }

    // "<Method>b__0" (lambda), "<Method>g__Local|0_0" (local function), "<Method>d__0" (state machine),
    // and their nestings such as "<<Method>g__Local|0_0>d" (state machine of a local function).
    private static bool IsGeneratedFor(string generatedName, string methodName)
    {
        if (!generatedName.StartsWith('<'))
        {
            return false;
        }

        var name = generatedName.AsSpan().TrimStart('<');

        return name.Length > methodName.Length
            && name[methodName.Length] == '>'
            && name.StartsWith(methodName, StringComparison.Ordinal);
    }

    internal sealed class TypeMembers
    {
        public MethodBase[] Own { get; }

        /// <summary>
        /// Compiler-generated nested types (closures and state machines) and their methods, at any
        /// depth: an async lambda that captures locals has its state machine inside its closure class.
        /// </summary>
        public (string Name, MethodBase[] Methods)[] Generated { get; }

        public TypeMembers(Type type)
        {
            Own = GetMethods(type);

            var generated = new List<(string, MethodBase[])>();
            AddGenerated(type, generated);
            Generated = generated.ToArray();
        }

        [UnconditionalSuppressMessage("Trimming", "IL2070",
            Justification = "Best effort: a member the trimmer removed is not warmed.")]
        private static void AddGenerated(Type type, List<(string, MethodBase[])> generated)
        {
            foreach (var nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (nested.Name.StartsWith('<') && !nested.ContainsGenericParameters)
                {
                    generated.Add((nested.Name, GetMethods(nested)));
                    AddGenerated(nested, generated);
                }
            }
        }

        [UnconditionalSuppressMessage("Trimming", "IL2070",
            Justification = "Best effort: a member the trimmer removed is not warmed.")]
        private static MethodBase[] GetMethods(Type type)
        {
            var methods = new List<MethodBase>();

            foreach (var method in type.GetMethods(DeclaredMembers))
            {
                if (!method.IsAbstract && !method.ContainsGenericParameters)
                {
                    methods.Add(method);
                }
            }

            foreach (var constructor in type.GetConstructors(DeclaredMembers))
            {
                // Static constructors are left alone so type initialisation keeps its normal timing.
                if (!constructor.IsStatic)
                {
                    methods.Add(constructor);
                }
            }

            return methods.ToArray();
        }
    }

    /// <summary>
    /// Maps a compiled method to its warm-up entry, or <see langword="null"/> when it cannot be warmed.
    /// </summary>
    internal static string? ToEntry(string typeName, string methodName)
    {
        if (!typeName.StartsWith("TUnit.Core.", StringComparison.Ordinal)
            && !typeName.StartsWith("TUnit.Engine.", StringComparison.Ordinal))
        {
            return null;
        }

        // Generic instantiations cannot be prepared from an open type.
        if (typeName.AsSpan().IndexOfAny('`', '[') >= 0 || typeName.StartsWith(typeof(JitWarmup).FullName!, StringComparison.Ordinal))
        {
            return null;
        }

        var generatedType = typeName.IndexOf("+<", StringComparison.Ordinal);

        if (generatedType >= 0)
        {
            // The innermost generated type decides: "Type+<>c__DisplayClass1_0+<<Method>b__0>d" is
            // the state machine of a lambda in Method.
            var nestedName = typeName[(typeName.LastIndexOf('+') + 1)..];
            typeName = typeName[..generatedType];

            if (!nestedName.StartsWith("<>", StringComparison.Ordinal))
            {
                // State machine "<Method>d__0": every method on it belongs to Method.
                methodName = nestedName;
            }
            else if (!methodName.StartsWith('<'))
            {
                // Closure class plumbing, such as its constructor.
                return null;
            }
        }

        if (methodName.StartsWith('<'))
        {
            // "<Method>b__0", or nested as in "<<Method>g__Local|0_0>d".
            var name = methodName.AsSpan().TrimStart('<');
            var end = name.IndexOf('>');

            if (end <= 0)
            {
                return null;
            }

            methodName = name[..end].ToString();
        }

        return typeName + Separator + methodName;
    }

    private sealed class Recorder : EventListener
    {
        private const string RuntimeEventSource = "Microsoft-Windows-DotNETRuntime";
        private const EventKeywords JitKeyword = (EventKeywords) 0x10;

        // Static because the base constructor can raise events before instance fields are assigned.
        private static readonly Lock s_lock = new();
        private static readonly HashSet<string> s_seen = [];
        private static readonly List<string> s_recorded = [];
        private static Recorder? s_instance;

        public static void Start(string path)
        {
            s_instance = new Recorder();

            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                try
                {
                    var membersByType = new Dictionary<Type, TypeMembers>();

                    lock (s_lock)
                    {
                        // Leave out what could not be warmed anyway, such as generic methods.
                        File.WriteAllLines(path, s_recorded.Where(entry => ResolveEntry(entry, membersByType).Any()));
                    }
                }
                catch (Exception exception)
                {
                    // A bad path must not fail the run or stop later exit handlers.
                    // Console.Error is intercepted for test output by now, so write to the real stream.
                    using var standardError = new StreamWriter(Console.OpenStandardError());
                    standardError.WriteLine($"Could not write {EnvironmentConstants.JitWarmupRecord} output to '{path}': {exception.Message}");
                }
            };
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == RuntimeEventSource)
            {
                EnableEvents(eventSource, EventLevel.Verbose, JitKeyword);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventName?.StartsWith("MethodLoadVerbose", StringComparison.Ordinal) != true
                || eventData.PayloadNames is not { } names
                || eventData.Payload is not { } payload)
            {
                return;
            }

            var typeIndex = names.IndexOf("MethodNamespace");
            var methodIndex = names.IndexOf("MethodName");

            if (typeIndex < 0 || methodIndex < 0
                || payload[typeIndex] is not string typeName
                || payload[methodIndex] is not string methodName
                || ToEntry(typeName, methodName) is not { } entry)
            {
                return;
            }

            lock (s_lock)
            {
                if (s_seen.Add(entry))
                {
                    s_recorded.Add(entry);
                }
            }
        }
    }
}
#endif
