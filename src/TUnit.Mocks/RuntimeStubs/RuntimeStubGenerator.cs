#if NET8_0_OR_GREATER
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

// The whole emitter sits behind RuntimeFeature.IsDynamicCodeSupported (checked in TryCreateStub):
// on Native AOT it is inert, and trimming cannot remove members off types the runtime observes in
// live calls here.
#pragma warning disable IL2026, IL2055, IL2067, IL2070, IL2072, IL2075, IL3050

namespace TUnit.Mocks.RuntimeStubs;

/// <summary>
/// Emits functional stub implementations of interfaces at runtime, closing the gap with
/// runtime-proxy mocking libraries for types the source generator structurally cannot see —
/// most importantly generic methods invoked by third-party code with type arguments that are
/// <c>internal</c> to another assembly (#6514).
///
/// Stubs are emitted into a dynamic assembly named <c>DynamicProxyGenAssembly2</c> carrying
/// Castle DynamicProxy's well-known public key: that is the exact identity the ecosystem already
/// grants <c>InternalsVisibleTo</c> for Moq/NSubstitute compatibility (e.g. the Azure Functions
/// Worker SDK), so any interface reachable by those libraries' proxies is reachable by these
/// stubs. Dynamic assemblies are never strong-name verified, so only the public key is needed.
///
/// Stub members return NSubstitute-style defaults (see <see cref="RuntimeStubDefaults"/>),
/// lazily and cached per member so identity is stable; properties round-trip set values. Stubs
/// are not configurable or verifiable — by definition their types cannot be named by the test.
/// </summary>
internal static class RuntimeStubGenerator
{
    // Castle DynamicProxy's well-known public key (the ecosystem's IVT grants name this blob).
    private const string DynamicProxyPublicKey =
        "0024000004800000940000000602000000240000525341310004000001000100c547cac37abd99c8" +
        "db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cfc0734aa39b93bf7852f7d9266654753c" +
        "c297e7d2edfe0bac1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92" +
        "d2d15605093924cceaf74c4861eff62abf69b9291ed0a340e113be11e6a7d3113e92484cf7045cc7";

    // Lazy values: GetOrAdd may invoke the factory once per contender for the SAME key and keep
    // only one result — a plain Type? value would let racing first-touches of one interface each
    // emit a permanent dynamic type before all but one are discarded. Lazy (ExecutionAndPublication)
    // guarantees exactly one emission per interface; cache hits stay lock-free.
    private static readonly ConcurrentDictionary<Type, Lazy<Type?>> _stubTypes = new();
    private static readonly object _emitLock = new();
    private static ModuleBuilder? _module;
    private static int _typeCounter;

    private static readonly MethodInfo GetTypeFromHandleMethod =
        typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), [typeof(RuntimeTypeHandle)])!;

    private static readonly MethodInfo GetReturnValueMethod =
        typeof(RuntimeStub).GetMethod("GetReturnValue", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly MethodInfo GetPropertyValueMethod =
        typeof(RuntimeStub).GetMethod("GetPropertyValue", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly MethodInfo SetPropertyValueMethod =
        typeof(RuntimeStub).GetMethod("SetPropertyValue", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly MethodInfo GetIndexerValueMethod =
        typeof(RuntimeStub).GetMethod("GetIndexerValue", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly MethodInfo SetIndexerValueMethod =
        typeof(RuntimeStub).GetMethod("SetIndexerValue", BindingFlags.Instance | BindingFlags.NonPublic)!;

    /// <summary>
    /// Tries to create a runtime stub implementing <paramref name="interfaceType"/>. Returns
    /// false — never throws — when stubs are disabled, dynamic code is unavailable (Native AOT),
    /// the type is not a stubbable interface, or emission fails (e.g. the interface is internal
    /// to an assembly that grants no <c>InternalsVisibleTo</c> the stub assembly can satisfy).
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Guarded by RuntimeFeature.IsDynamicCodeSupported; on Native AOT this method returns false before reaching emission.")]
    public static bool TryCreateStub(Type interfaceType, [NotNullWhen(true)] out IMock? stub)
    {
        stub = null;

        if (!TUnitMocksSettings.Default.RuntimeAutoStubs || !RuntimeFeature.IsDynamicCodeSupported)
        {
            return false;
        }

        if (!interfaceType.IsInterface || interfaceType.IsGenericTypeDefinition || interfaceType.ContainsGenericParameters)
        {
            return false;
        }

        var stubType = _stubTypes.GetOrAdd(interfaceType, static t => new Lazy<Type?>(() =>
        {
            try
            {
                return EmitStubType(t);
            }
            catch
            {
                // Inaccessible interface (no matching IVT grant), unsupported member shape, or
                // any other emission failure: remember the miss so we never retry, and let the
                // engine fall back to its existing default (null).
                return null;
            }
        })).Value;

        if (stubType is null)
        {
            return false;
        }

        stub = new RuntimeStubMock((RuntimeStub)Activator.CreateInstance(stubType)!);
        return true;
    }

    [RequiresDynamicCode("Emits stub types at runtime; callers are gated on RuntimeFeature.IsDynamicCodeSupported.")]
    private static Type? EmitStubType(Type interfaceType)
    {
        var interfaces = CollectInterfaces(interfaceType);

        foreach (var iface in interfaces)
        {
            if (!IsSupported(iface))
            {
                return null;
            }
        }

        // ConcurrentDictionary.GetOrAdd only de-duplicates the factory per KEY — factories for
        // different interfaces run concurrently, and ModuleBuilder.DefineType/TypeBuilder.CreateType
        // on the shared module are not thread-safe on coreclr (dotnet/runtime#64094). Serialize
        // emission, as Castle's ModuleScope does for this same assembly identity; cache hits in
        // TryCreateStub stay lock-free.
        lock (_emitLock)
        {
            var module = EnsureModule();
            var tb = module.DefineType(
                $"TUnit.Mocks.RuntimeStubs.Stub{Interlocked.Increment(ref _typeCounter)}_{Sanitize(interfaceType.Name)}",
                TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class,
                typeof(RuntimeStub));

            foreach (var iface in interfaces)
            {
                tb.AddInterfaceImplementation(iface);
            }

            EmitConstructor(tb);

            var slot = 0;
            foreach (var iface in interfaces)
            {
                var handledAccessors = new HashSet<MethodInfo>();

                foreach (var property in iface.GetProperties())
                {
                    var propertySlot = slot++;
                    var isRefLike = property.PropertyType.IsByRefLike || property.PropertyType.IsPointer;
                    // Index arguments are boxed into the state key so distinct indices round-trip
                    // independently — unless an index parameter cannot be boxed (by-ref / ref struct
                    // / pointer), in which case the accessor degrades to slot-level state.
                    var indexParameters = property.GetIndexParameters();
                    var indexable = indexParameters.Length > 0 && indexParameters.All(static p =>
                        !p.ParameterType.IsByRef && !p.ParameterType.IsByRefLike && !p.ParameterType.IsPointer);

                    if (property.GetMethod is { IsAbstract: true } getter)
                    {
                        handledAccessors.Add(getter);
                        EmitAccessor(tb, iface, getter, propertySlot, isSetter: false, isRefLike, indexable);
                    }

                    if (property.SetMethod is { IsAbstract: true } setter)
                    {
                        handledAccessors.Add(setter);
                        EmitAccessor(tb, iface, setter, propertySlot, isSetter: true, isRefLike, indexable);
                    }
                }

                foreach (var evt in iface.GetEvents())
                {
                    if (evt.AddMethod is { IsAbstract: true } add)
                    {
                        handledAccessors.Add(add);
                        EmitNoOp(tb, iface, add, slot++);
                    }

                    if (evt.RemoveMethod is { IsAbstract: true } remove)
                    {
                        handledAccessors.Add(remove);
                        EmitNoOp(tb, iface, remove, slot++);
                    }
                }

                foreach (var method in iface.GetMethods())
                {
                    // Default interface methods keep their bodies; accessors were handled above.
                    if (!method.IsAbstract || handledAccessors.Contains(method))
                    {
                        continue;
                    }

                    EmitMethod(tb, iface, method, slot++);
                }
            }

            return tb.CreateType();
        }
    }

    private static Type[] CollectInterfaces(Type interfaceType)
    {
        var inherited = interfaceType.GetInterfaces();
        var all = new Type[inherited.Length + 1];
        all[0] = interfaceType;
        Array.Copy(inherited, 0, all, 1, inherited.Length);
        return all;
    }

    private static bool IsSupported(Type iface)
    {
        foreach (var method in iface.GetMethods())
        {
            // Static abstract members need the implementing type itself to provide statics the
            // engine can never dispatch; by-ref returns have no storable default to hand back.
            if (method.IsStatic && method.IsAbstract)
            {
                return false;
            }

            if (method.IsAbstract && method.ReturnType.IsByRef)
            {
                return false;
            }
        }

        return true;
    }

    private static ModuleBuilder EnsureModule()
    {
        if (_module is { } existing)
        {
            return existing;
        }

        var assemblyName = new AssemblyName("DynamicProxyGenAssembly2");
        assemblyName.SetPublicKey(Convert.FromHexString(DynamicProxyPublicKey));
        var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("TUnit.Mocks.RuntimeStubs");
        return Interlocked.CompareExchange(ref _module, module, null) ?? module;
    }

    private static void EmitConstructor(TypeBuilder tb)
    {
        var ctor = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
        var il = ctor.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(RuntimeStub).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, Type.EmptyTypes)!);
        il.Emit(OpCodes.Ret);
    }

    private const MethodAttributes ExplicitImplAttributes =
        MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig |
        MethodAttributes.Virtual | MethodAttributes.NewSlot;

    private static MethodBuilder DefineExplicitImpl(TypeBuilder tb, Type iface, MethodInfo target, int slot,
        out Type[] parameterTypes, out Type returnType, out Dictionary<Type, Type> genericMap)
    {
        var mb = tb.DefineMethod($"<{slot}>{iface.Name}.{target.Name}", ExplicitImplAttributes);

        genericMap = new Dictionary<Type, Type>();
        if (target.IsGenericMethodDefinition)
        {
            var sourceArgs = target.GetGenericArguments();
            var names = new string[sourceArgs.Length];
            for (var i = 0; i < sourceArgs.Length; i++)
            {
                names[i] = sourceArgs[i].Name;
            }

            var builders = mb.DefineGenericParameters(names);
            for (var i = 0; i < sourceArgs.Length; i++)
            {
                genericMap[sourceArgs[i]] = builders[i];
            }

            for (var i = 0; i < sourceArgs.Length; i++)
            {
                var source = sourceArgs[i];
                builders[i].SetGenericParameterAttributes(
                    source.GenericParameterAttributes & ~GenericParameterAttributes.VarianceMask);

                Type? baseConstraint = null;
                var interfaceConstraints = new List<Type>();
                foreach (var constraint in source.GetGenericParameterConstraints())
                {
                    var mapped = Substitute(constraint, genericMap);
                    if (mapped.IsInterface || mapped.IsGenericParameter)
                    {
                        interfaceConstraints.Add(mapped);
                    }
                    else
                    {
                        baseConstraint = mapped;
                    }
                }

                if (baseConstraint is not null)
                {
                    builders[i].SetBaseTypeConstraint(baseConstraint);
                }

                if (interfaceConstraints.Count > 0)
                {
                    builders[i].SetInterfaceConstraints(interfaceConstraints.ToArray());
                }
            }
        }

        var parameters = target.GetParameters();
        parameterTypes = new Type[parameters.Length];
        var parameterRequiredModifiers = new Type[parameters.Length][];
        var parameterOptionalModifiers = new Type[parameters.Length][];
        for (var i = 0; i < parameters.Length; i++)
        {
            parameterTypes[i] = Substitute(parameters[i].ParameterType, genericMap);
            parameterRequiredModifiers[i] = parameters[i].GetRequiredCustomModifiers();
            parameterOptionalModifiers[i] = parameters[i].GetOptionalCustomModifiers();
        }

        returnType = Substitute(target.ReturnType, genericMap);
        // Custom modifiers are part of the CLR signature the MethodImpl must match — an init
        // setter's modreq(IsExternalInit) or an `in` parameter's modreq(InAttribute) dropped
        // here would make CreateType reject the implementation.
        mb.SetSignature(
            returnType,
            target.ReturnParameter.GetRequiredCustomModifiers(),
            target.ReturnParameter.GetOptionalCustomModifiers(),
            parameterTypes,
            parameterRequiredModifiers,
            parameterOptionalModifiers);
        tb.DefineMethodOverride(mb, target);
        return mb;
    }

    private static void EmitMethod(TypeBuilder tb, Type iface, MethodInfo method, int slot)
    {
        var mb = DefineExplicitImpl(tb, iface, method, slot, out var parameterTypes, out var returnType, out _);
        var il = mb.GetILGenerator();

        EmitOutParameterInit(il, method, parameterTypes);
        EmitReturn(il, slot, returnType, GetReturnValueMethod);
    }

    private static void EmitAccessor(TypeBuilder tb, Type iface, MethodInfo accessor, int slot, bool isSetter, bool isRefLike, bool indexable)
    {
        var mb = DefineExplicitImpl(tb, iface, accessor, slot, out var parameterTypes, out _, out _);
        var il = mb.GetILGenerator();

        if (isSetter)
        {
            // Ref-struct / pointer values cannot be boxed for storage — the setter is a no-op.
            if (!isRefLike)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, slot);
                if (indexable)
                {
                    EmitLoadArgsArray(il, parameterTypes, count: parameterTypes.Length - 1);
                }

                il.Emit(OpCodes.Ldarg, parameterTypes.Length); // value is the last argument
                var valueType = parameterTypes[^1];
                if (valueType.IsValueType || valueType.IsGenericParameter)
                {
                    il.Emit(OpCodes.Box, valueType);
                }

                il.Emit(OpCodes.Call, indexable ? SetIndexerValueMethod : SetPropertyValueMethod);
            }

            il.Emit(OpCodes.Ret);
            return;
        }

        if (indexable && !isRefLike)
        {
            var returnType = Substitute(accessor.ReturnType, []);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, slot);
            EmitLoadArgsArray(il, parameterTypes, count: parameterTypes.Length);
            il.Emit(OpCodes.Ldtoken, returnType);
            il.Emit(OpCodes.Call, GetTypeFromHandleMethod);
            il.Emit(OpCodes.Call, GetIndexerValueMethod);
            il.Emit(OpCodes.Unbox_Any, returnType);
            il.Emit(OpCodes.Ret);
            return;
        }

        EmitReturn(il, slot, Substitute(accessor.ReturnType, []), GetPropertyValueMethod);
    }

    /// <summary>Loads a new object?[] holding the first <paramref name="count"/> arguments
    /// (boxing value types), leaving it on the evaluation stack.</summary>
    private static void EmitLoadArgsArray(ILGenerator il, Type[] parameterTypes, int count)
    {
        il.Emit(OpCodes.Ldc_I4, count);
        il.Emit(OpCodes.Newarr, typeof(object));
        for (var i = 0; i < count; i++)
        {
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Emit(OpCodes.Ldarg, i + 1);
            if (parameterTypes[i].IsValueType || parameterTypes[i].IsGenericParameter)
            {
                il.Emit(OpCodes.Box, parameterTypes[i]);
            }

            il.Emit(OpCodes.Stelem_Ref);
        }
    }

    private static void EmitNoOp(TypeBuilder tb, Type iface, MethodInfo method, int slot)
    {
        var mb = DefineExplicitImpl(tb, iface, method, slot, out _, out _, out _);
        mb.GetILGenerator().Emit(OpCodes.Ret);
    }

    private static void EmitOutParameterInit(ILGenerator il, MethodInfo method, Type[] parameterTypes)
    {
        var parameters = method.GetParameters();
        for (var i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].IsOut && parameterTypes[i].IsByRef)
            {
                il.Emit(OpCodes.Ldarg, i + 1);
                il.Emit(OpCodes.Initobj, parameterTypes[i].GetElementType()!);
            }
        }
    }

    private static void EmitReturn(ILGenerator il, int slot, Type returnType, MethodInfo valueSource)
    {
        if (returnType == typeof(void))
        {
            il.Emit(OpCodes.Ret);
            return;
        }

        if (returnType.IsPointer || returnType.IsFunctionPointer)
        {
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Conv_U);
            il.Emit(OpCodes.Ret);
            return;
        }

        if (returnType.IsByRefLike)
        {
            var local = il.DeclareLocal(returnType);
            il.Emit(OpCodes.Ldloca, local);
            il.Emit(OpCodes.Initobj, returnType);
            il.Emit(OpCodes.Ldloc, local);
            il.Emit(OpCodes.Ret);
            return;
        }

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4, slot);
        il.Emit(OpCodes.Ldtoken, returnType);
        il.Emit(OpCodes.Call, GetTypeFromHandleMethod);
        il.Emit(OpCodes.Call, valueSource);
        il.Emit(OpCodes.Unbox_Any, returnType);
        il.Emit(OpCodes.Ret);
    }

    /// <summary>
    /// Rebinds a type that may reference the source method's generic parameters onto the
    /// stub method's own generic parameter builders.
    /// </summary>
    private static Type Substitute(Type type, Dictionary<Type, Type> map)
    {
        if (map.Count == 0)
        {
            return type;
        }

        if (map.TryGetValue(type, out var mapped))
        {
            return mapped;
        }

        if (type.IsByRef)
        {
            return Substitute(type.GetElementType()!, map).MakeByRefType();
        }

        if (type.IsArray)
        {
            var element = Substitute(type.GetElementType()!, map);
            var rank = type.GetArrayRank();
            return rank == 1 ? element.MakeArrayType() : element.MakeArrayType(rank);
        }

        if (type.IsPointer)
        {
            return Substitute(type.GetElementType()!, map).MakePointerType();
        }

        if (type.IsConstructedGenericType)
        {
            var args = type.GetGenericArguments();
            var changed = false;
            for (var i = 0; i < args.Length; i++)
            {
                var substituted = Substitute(args[i], map);
                if (!ReferenceEquals(substituted, args[i]))
                {
                    args[i] = substituted;
                    changed = true;
                }
            }

            return changed ? type.GetGenericTypeDefinition().MakeGenericType(args) : type;
        }

        return type;
    }

    private static string Sanitize(string name)
    {
        var chars = name.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_')
            {
                chars[i] = '_';
            }
        }

        return new string(chars);
    }
}
#else
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Mocks.RuntimeStubs;

/// <summary>Runtime stub emission requires Reflection.Emit; unavailable on this target.</summary>
internal static class RuntimeStubGenerator
{
    public static bool TryCreateStub(Type interfaceType, [NotNullWhen(true)] out IMock? stub)
    {
        stub = null;
        return false;
    }
}
#endif
