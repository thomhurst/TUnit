using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Mocks.RuntimeStubs;

/// <summary>
/// Computes NSubstitute-style default values for members of runtime-emitted stubs: empty strings,
/// completed tasks, empty collections, recursive auto-stubs for interfaces, zeroed value types.
/// Only reachable from the runtime-stub path, which is itself gated on
/// <c>RuntimeFeature.IsDynamicCodeSupported</c> — never on Native AOT.
/// </summary>
internal static class RuntimeStubDefaults
{
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Only reachable when RuntimeFeature.IsDynamicCodeSupported (guarded in RuntimeStubGenerator).")]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Operates on runtime types observed in live calls; the stub path is inert when members were trimmed.")]
    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "Constructed over runtime-observed type arguments on the guarded stub path.")]
    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Constructed over runtime-observed type arguments on the guarded stub path.")]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Constructed over runtime-observed type arguments on the guarded stub path.")]
    public static object? GetDefault(Type type)
    {
        if (type == typeof(string))
        {
            return string.Empty;
        }

        if (type == typeof(Task))
        {
            return Task.CompletedTask;
        }

        if (type == typeof(ValueTask))
        {
            return default(ValueTask);
        }

        if (type.IsConstructedGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            var typeArgs = type.GetGenericArguments();

            if (definition == typeof(Task<>))
            {
                return typeof(Task).GetMethod(nameof(Task.FromResult))!
                    .MakeGenericMethod(typeArgs[0])
                    .Invoke(null, [GetDefault(typeArgs[0])]);
            }

            if (definition == typeof(ValueTask<>))
            {
                // Explicit ctor selection: ValueTask<T> has both (T) and (Task<T>) constructors,
                // and a null argument (reference-typed T with a null default) matches either, so
                // Activator.CreateInstance(type, arg) would be ambiguous.
                return type.GetConstructor([typeArgs[0]])!.Invoke([GetDefault(typeArgs[0])]);
            }

            if (definition == typeof(Nullable<>))
            {
                return null;
            }

            if (definition == typeof(IEnumerable<>)
                || definition == typeof(IReadOnlyCollection<>)
                || definition == typeof(IReadOnlyList<>)
                || definition == typeof(ICollection<>)
                || definition == typeof(IList<>)
                || definition == typeof(List<>))
            {
                return Activator.CreateInstance(typeof(List<>).MakeGenericType(typeArgs[0]));
            }

            if (definition == typeof(IDictionary<,>)
                || definition == typeof(IReadOnlyDictionary<,>)
                || definition == typeof(Dictionary<,>))
            {
                return Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeArgs));
            }
        }

        if (type.IsArray && type.GetArrayRank() == 1)
        {
            // Array element types can never be by-ref-like, so this is safe for any array.
            return Array.CreateInstance(type.GetElementType()!, 0);
        }

        if (type == typeof(IEnumerable))
        {
            return Array.Empty<object>();
        }

        if (type.IsInterface)
        {
            // A source-generated mock factory beats a runtime stub: it produces a fully
            // configurable Mock<T> the user can retrieve via Mock.Get.
            if (MockRegistry.TryCreateAutoMock(type, MockBehavior.Loose, out var registered))
            {
                return registered.ObjectInstance;
            }

            if (RuntimeStubGenerator.TryCreateStub(type, out var stub))
            {
                return stub.ObjectInstance;
            }

            return null;
        }

        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }

        return null;
    }
}
