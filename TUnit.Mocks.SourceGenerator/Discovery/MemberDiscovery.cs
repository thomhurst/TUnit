using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Mocks.SourceGenerator.Extensions;
using TUnit.Mocks.SourceGenerator.Models;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace TUnit.Mocks.SourceGenerator.Discovery;

/// <summary>
/// Enumerates all mockable members of a type symbol.
/// Walks AllInterfaces for inherited members, detects overlapping signatures,
/// and handles default interface methods, properties, events, and indexers.
/// </summary>
internal static class MemberDiscovery
{
    public static (EquatableArray<MockMemberModel> Methods, EquatableArray<MockMemberModel> Properties, EquatableArray<MockEventModel> Events)
        DiscoverMembers(ITypeSymbol typeSymbol, IAssemblySymbol? compilationAssembly = null)
    {
        var methods = new List<MockMemberModel>();
        var properties = new List<MockMemberModel>();
        var events = new List<MockEventModel>();

        var seenMethods = new HashSet<string>();
        var seenProperties = new Dictionary<string, int?>();
        var seenEvents = new HashSet<string>();

        int memberIdCounter = 0;

        // Collect all interfaces to scan
        var interfaces = typeSymbol.TypeKind == TypeKind.Interface
            ? new[] { typeSymbol }.Concat(typeSymbol.AllInterfaces)
            : typeSymbol.AllInterfaces.AsEnumerable();

        // If it's a class, also include its own members
        if (typeSymbol.TypeKind == TypeKind.Class)
        {
            ProcessClassMembers(typeSymbol, compilationAssembly, methods, properties, events,
                seenMethods, seenProperties, seenEvents, ref memberIdCounter);
        }

        foreach (var iface in interfaces)
        {
            string? explicitInterfaceName = null;
            var interfaceFqn = iface.GetFullyQualifiedName();

            foreach (var member in iface.GetMembers())
            {
                if (member.IsStatic)
                {
                    if (member.IsAbstract)
                    {
                        CollectStaticAbstractMember(member, interfaceFqn, methods, properties, events, seenMethods, seenProperties, seenEvents, ref memberIdCounter);
                    }
                    continue;
                }

                switch (member)
                {
                    case IMethodSymbol method when method.MethodKind == MethodKind.Ordinary:
                    {
                        var key = GetMethodKey(method);
                        if (!seenMethods.Add(key)) continue;

                        methods.Add(CreateMethodModel(method, ref memberIdCounter, explicitInterfaceName, interfaceFqn));
                        break;
                    }

                    case IPropertySymbol property when !property.IsIndexer:
                    {
                        var key = $"P:{property.Name}";
                        if (seenProperties.TryGetValue(key, out var existingIndex))
                        {
                            if (existingIndex.HasValue)
                            {
                                MergePropertyAccessors(properties, existingIndex.Value, property, ref memberIdCounter);
                            }
                        }
                        else
                        {
                            seenProperties[key] = properties.Count;
                            properties.Add(CreatePropertyModel(property, ref memberIdCounter, explicitInterfaceName, interfaceFqn));
                        }
                        break;
                    }

                    case IPropertySymbol indexer when indexer.IsIndexer:
                    {
                        var paramTypes = string.Join(',', indexer.Parameters.Select(p => p.Type.GetFullyQualifiedName()));
                        var key = $"I:[{paramTypes}]";
                        if (seenProperties.TryGetValue(key, out var existingIndex))
                        {
                            if (existingIndex.HasValue)
                            {
                                MergePropertyAccessors(properties, existingIndex.Value, indexer, ref memberIdCounter);
                            }
                        }
                        else
                        {
                            seenProperties[key] = properties.Count;
                            properties.Add(CreateIndexerModel(indexer, ref memberIdCounter, explicitInterfaceName, interfaceFqn));
                        }
                        break;
                    }

                    case IEventSymbol evt:
                    {
                        var key = $"E:{evt.Name}";
                        if (!seenEvents.Add(key)) continue;

                        events.Add(CreateEventModel(evt, explicitInterfaceName, interfaceFqn));
                        break;
                    }
                }
            }
        }

        return (
            new EquatableArray<MockMemberModel>(methods.ToImmutableArray()),
            new EquatableArray<MockMemberModel>(properties.ToImmutableArray()),
            new EquatableArray<MockEventModel>(events.ToImmutableArray())
        );
    }

    /// <summary>
    /// Discovers members from multiple type symbols, merging and deduplicating across all.
    /// Used for multi-interface mocks like Mock.Of&lt;T1, T2&gt;().
    /// </summary>
    public static (EquatableArray<MockMemberModel> Methods, EquatableArray<MockMemberModel> Properties, EquatableArray<MockEventModel> Events)
        DiscoverMembersFromMultipleTypes(INamedTypeSymbol[] typeSymbols, IAssemblySymbol? compilationAssembly = null)
    {
        var methods = new List<MockMemberModel>();
        var properties = new List<MockMemberModel>();
        var events = new List<MockEventModel>();

        var seenMethods = new HashSet<string>();
        var seenProperties = new Dictionary<string, int?>();
        var seenEvents = new HashSet<string>();

        int memberIdCounter = 0;

        foreach (var typeSymbol in typeSymbols)
        {
            // Collect all interfaces to scan (the type itself + its inherited interfaces)
            var interfaces = typeSymbol.TypeKind == TypeKind.Interface
                ? new[] { typeSymbol }.Concat(typeSymbol.AllInterfaces)
                : typeSymbol.AllInterfaces.AsEnumerable();

            foreach (var iface in interfaces)
            {
                var interfaceFqn = iface.GetFullyQualifiedName();

                foreach (var member in iface.GetMembers())
                {
                    if (member.IsStatic)
                    {
                        if (member.IsAbstract)
                        {
                            CollectStaticAbstractMember(member, interfaceFqn, methods, properties, events, seenMethods, seenProperties, seenEvents, ref memberIdCounter);
                        }
                        continue;
                    }

                    switch (member)
                    {
                        case IMethodSymbol method when method.MethodKind == MethodKind.Ordinary:
                        {
                            var key = GetMethodKey(method);
                            if (!seenMethods.Add(key)) continue;
                            methods.Add(CreateMethodModel(method, ref memberIdCounter, null, declaringInterfaceName: interfaceFqn));
                            break;
                        }

                        case IPropertySymbol property when !property.IsIndexer:
                        {
                            var key = $"P:{property.Name}";
                            if (seenProperties.TryGetValue(key, out var existingIndex))
                            {
                                if (existingIndex.HasValue)
                                {
                                    MergePropertyAccessors(properties, existingIndex.Value, property, ref memberIdCounter);
                                }
                            }
                            else
                            {
                                seenProperties[key] = properties.Count;
                                properties.Add(CreatePropertyModel(property, ref memberIdCounter, null, declaringInterfaceName: interfaceFqn));
                            }
                            break;
                        }

                        case IPropertySymbol indexer when indexer.IsIndexer:
                        {
                            var paramTypes = string.Join(',', indexer.Parameters.Select(p => p.Type.GetFullyQualifiedName()));
                            var key = $"I:[{paramTypes}]";
                            if (seenProperties.TryGetValue(key, out var existingIndex))
                            {
                                if (existingIndex.HasValue)
                                {
                                    MergePropertyAccessors(properties, existingIndex.Value, indexer, ref memberIdCounter);
                                }
                            }
                            else
                            {
                                seenProperties[key] = properties.Count;
                                properties.Add(CreateIndexerModel(indexer, ref memberIdCounter, null, declaringInterfaceName: interfaceFqn));
                            }
                            break;
                        }

                        case IEventSymbol evt:
                        {
                            var key = $"E:{evt.Name}";
                            if (!seenEvents.Add(key)) continue;
                            events.Add(CreateEventModel(evt, null, declaringInterfaceName: interfaceFqn));
                            break;
                        }
                    }
                }
            }
        }

        return (
            new EquatableArray<MockMemberModel>(methods.ToImmutableArray()),
            new EquatableArray<MockMemberModel>(properties.ToImmutableArray()),
            new EquatableArray<MockEventModel>(events.ToImmutableArray())
        );
    }

    private static void ProcessClassMembers(
        ITypeSymbol typeSymbol,
        IAssemblySymbol? compilationAssembly,
        List<MockMemberModel> methods,
        List<MockMemberModel> properties,
        List<MockEventModel> events,
        HashSet<string> seenMethods,
        Dictionary<string, int?> seenProperties,
        HashSet<string> seenEvents,
        ref int memberIdCounter)
    {
        // Walk up the class hierarchy
        var current = typeSymbol;
        while (current != null && current.SpecialType != SpecialType.System_Object)
        {
            // Pre-compute per-level: are internal members of this type accessible?
            // Avoids repeated SymbolEqualityComparer + GivesAccessTo checks per member.
            var isExternalType = compilationAssembly is not null
                && !SymbolEqualityComparer.Default.Equals(current.ContainingAssembly, compilationAssembly);
            var hasInternalAccess = isExternalType
                && compilationAssembly is not null
                && current.ContainingAssembly.GivesAccessTo(compilationAssembly);

            foreach (var member in current.GetMembers())
            {
                if (member.IsStatic) continue;

                // Skip private members and sealed overrides (can't override them)
                if (member.DeclaredAccessibility == Accessibility.Private) continue;
                if (member is IMethodSymbol { IsSealed: true }) continue;
                if (member is IPropertySymbol { IsSealed: true }) continue;

                // Skip members inaccessible from the compilation assembly
                // (e.g., internal virtual methods from external assemblies like Azure SDK)
                if (isExternalType && !IsMemberAccessibleFromExternal(member, compilationAssembly!, hasInternalAccess)) continue;

                // Non-virtual members are recorded in the seen-sets (but not collected) so that
                // base virtuals hidden by 'new' in a derived class are not emitted as overrides.
                switch (member)
                {
                    case IMethodSymbol method when method.MethodKind == MethodKind.Ordinary:
                    {
                        var key = GetMethodKey(method);
                        if (method.IsAbstract || method.IsVirtual || method.IsOverride)
                        {
                            if (!seenMethods.Add(key)) continue;
                            methods.Add(CreateMethodModel(method, ref memberIdCounter, null));
                        }
                        else
                        {
                            seenMethods.Add(key);
                        }
                        break;
                    }

                    case IPropertySymbol property when !property.IsIndexer:
                    {
                        var key = $"P:{property.Name}";
                        if (property.IsAbstract || property.IsVirtual || property.IsOverride)
                        {
                            if (seenProperties.TryGetValue(key, out var existingIndex))
                            {
                                if (existingIndex.HasValue)
                                {
                                    MergePropertyAccessors(properties, existingIndex.Value, property, ref memberIdCounter);
                                }
                            }
                            else
                            {
                                seenProperties[key] = properties.Count;
                                properties.Add(CreatePropertyModel(property, ref memberIdCounter, null));
                            }
                        }
                        else if (!seenProperties.ContainsKey(key))
                        {
                            seenProperties[key] = null;
                        }
                        break;
                    }

                    case IPropertySymbol indexer when indexer.IsIndexer:
                    {
                        var paramTypes = string.Join(',', indexer.Parameters.Select(p => p.Type.GetFullyQualifiedName()));
                        var key = $"I:[{paramTypes}]";
                        if (indexer.IsAbstract || indexer.IsVirtual || indexer.IsOverride)
                        {
                            if (seenProperties.TryGetValue(key, out var existingIndex))
                            {
                                if (existingIndex.HasValue)
                                {
                                    MergePropertyAccessors(properties, existingIndex.Value, indexer, ref memberIdCounter);
                                }
                            }
                            else
                            {
                                seenProperties[key] = properties.Count;
                                properties.Add(CreateIndexerModel(indexer, ref memberIdCounter, null));
                            }
                        }
                        else if (!seenProperties.ContainsKey(key))
                        {
                            seenProperties[key] = null;
                        }
                        break;
                    }

                    case IEventSymbol evt:
                    {
                        var key = $"E:{evt.Name}";
                        if (evt.IsAbstract || evt.IsVirtual || evt.IsOverride)
                        {
                            if (!seenEvents.Add(key)) continue;
                            events.Add(CreateEventModel(evt, null));
                        }
                        else
                        {
                            seenEvents.Add(key);
                        }
                        break;
                    }
                }
            }

            current = current.BaseType;
        }
    }

    /// <summary>
    /// Checks accessibility for a member known to be from an external assembly.
    /// Used in the hot loop where assembly identity is pre-computed per hierarchy level.
    /// </summary>
    private static bool IsMemberAccessibleFromExternal(ISymbol member, IAssemblySymbol compilationAssembly, bool hasInternalAccess)
    {
        var accessibility = member.DeclaredAccessibility;
        // ProtectedOrInternal (protected internal) is intentionally NOT blocked here:
        // the generated mock subclasses the target, so the protected part grants access.
        // ProtectedAndInternal (private protected) requires BOTH inheritance AND internal access.
        if (accessibility is Accessibility.Internal or Accessibility.ProtectedAndInternal)
        {
            if (!hasInternalAccess)
                return false;
        }

        return AreMemberSignatureTypesAccessible(member, compilationAssembly);
    }

    /// <summary>
    /// Full accessibility check for members where the containing assembly isn't pre-computed
    /// (used by DiscoverConstructors).
    /// </summary>
    private static bool IsMemberAccessible(ISymbol member, IAssemblySymbol? compilationAssembly)
    {
        if (compilationAssembly is null) return true;

        var memberAssembly = member.ContainingAssembly;
        if (SymbolEqualityComparer.Default.Equals(memberAssembly, compilationAssembly))
            return true;

        var accessibility = member.DeclaredAccessibility;
        if (accessibility is Accessibility.Internal or Accessibility.ProtectedAndInternal)
        {
            if (!memberAssembly.GivesAccessTo(compilationAssembly))
                return false;
        }

        return AreMemberSignatureTypesAccessible(member, compilationAssembly);
    }

    private static bool AreMemberSignatureTypesAccessible(ISymbol member, IAssemblySymbol compilationAssembly)
    {
        switch (member)
        {
            case IMethodSymbol method:
                if (!IsTypeAccessible(method.ReturnType, compilationAssembly)) return false;
                foreach (var param in method.Parameters)
                {
                    if (!IsTypeAccessible(param.Type, compilationAssembly)) return false;
                }
                return true;

            case IPropertySymbol property:
                return IsTypeAccessible(property.Type, compilationAssembly);

            case IEventSymbol evt:
                return IsTypeAccessible(evt.Type, compilationAssembly);

            default:
                return true;
        }
    }

    private static bool IsTypeAccessible(ITypeSymbol type, IAssemblySymbol compilationAssembly)
    {
        // Type parameters are always accessible
        if (type is ITypeParameterSymbol) return true;

        // Pointer types can't appear in mock override signatures (even same-assembly)
        if (type is IPointerTypeSymbol or IFunctionPointerTypeSymbol) return false;

        // Arrays: check element type
        if (type is IArrayTypeSymbol arrayType)
            return IsTypeAccessible(arrayType.ElementType, compilationAssembly);

        // Check the type itself before recursing into type arguments (short-circuits early)
        var typeAssembly = type.ContainingAssembly;
        if (typeAssembly is not null
            && !SymbolEqualityComparer.Default.Equals(typeAssembly, compilationAssembly))
        {
            var accessibility = type.DeclaredAccessibility;
            if (accessibility == Accessibility.Private) return false;
            if (accessibility is Accessibility.Internal or Accessibility.ProtectedAndInternal)
            {
                if (!typeAssembly.GivesAccessTo(compilationAssembly)) return false;
            }
        }

        // For generic types, also check type arguments
        if (type is INamedTypeSymbol namedType)
        {
            foreach (var typeArg in namedType.TypeArguments)
            {
                if (!IsTypeAccessible(typeArg, compilationAssembly)) return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Creates a MockMemberModel from a delegate type's Invoke method.
    /// </summary>
    public static MockMemberModel CreateDelegateInvokeModel(IMethodSymbol invokeMethod, ref int memberIdCounter)
    {
        return CreateMethodModel(invokeMethod, ref memberIdCounter, null);
    }

    private static MockMemberModel CreateMethodModel(IMethodSymbol method, ref int memberIdCounter, string? explicitInterfaceName, string? declaringInterfaceName = null)
    {
        var returnType = method.ReturnType;
        var isAsync = returnType.IsAsyncReturnType();
        var isValueTask = returnType.IsValueTaskReturnType();
        var (unwrappedType, isVoidAsync) = returnType.GetUnwrappedReturnType();
        var isVoid = method.ReturnsVoid || isVoidAsync;

        // Check if the effective return type (unwrapped for async) is an interface with
        // static abstract members. Such types cannot be used as generic type arguments (CS8920).
        var effectiveReturnTypeSymbol = returnType.GetAsyncInnerTypeSymbol() ?? returnType;
        var returnTypeHasStaticAbstract = !isVoid && IsInterfaceWithStaticAbstractMembers(effectiveReturnTypeSymbol);

        return new MockMemberModel
        {
            Name = method.Name,
            MemberId = memberIdCounter++,
            ReturnType = returnType.GetFullyQualifiedNameWithNullability(),
            UnwrappedReturnType = unwrappedType,
            IsVoid = isVoid,
            IsAsync = isAsync,
            IsValueTask = isValueTask,
            IsProperty = false,
            IsGenericMethod = method.IsGenericMethod,
            Parameters = new EquatableArray<MockParameterModel>(
                method.Parameters.Select(p => new MockParameterModel
                {
                    Name = EscapeIdentifier(p.Name),
                    Type = p.Type.GetMinimallyQualifiedNameWithNullability(),
                    FullyQualifiedType = p.Type.GetFullyQualifiedNameWithNullability(),
                    Direction = p.GetParameterDirection(),
                    HasDefaultValue = p.HasExplicitDefaultValue,
                    DefaultValueExpression = p.HasExplicitDefaultValue ? FormatDefaultValue(p) : null,
                    IsValueType = p.Type.IsValueType,
                    IsRefStruct = p.Type.IsRefLikeType,
                    SpanElementType = GetSpanElementType(p.Type)
                }).ToImmutableArray()
            ),
            TypeParameters = new EquatableArray<MockTypeParameterModel>(
                method.TypeParameters.Select(tp => new MockTypeParameterModel
                {
                    Name = tp.Name,
                    Constraints = tp.GetGenericConstraints()
                }).ToImmutableArray()
            ),
            ExplicitInterfaceName = explicitInterfaceName,
            DeclaringInterfaceName = declaringInterfaceName,
            NullableAnnotation = returnType.NullableAnnotation.ToString(),
            SmartDefault = isVoid ? "" : returnType.GetSmartDefault(returnType.IsNullableAnnotated()),
            UnwrappedSmartDefault = ComputeUnwrappedSmartDefault(returnType, isVoid, isAsync),
            IsAbstractMember = method.IsAbstract,
            IsVirtualMember = method.IsVirtual || method.IsOverride,
            IsProtected = method.DeclaredAccessibility == Accessibility.Protected
                       || method.DeclaredAccessibility == Accessibility.ProtectedOrInternal,
            IsRefStructReturn = returnType.IsRefLikeType,
            IsReturnTypeStaticAbstractInterface = returnTypeHasStaticAbstract,
            SpanReturnElementType = returnType.IsRefLikeType ? GetSpanElementType(returnType) : null
        };
    }

    /// <summary>
    /// When a property with the same name appears from multiple interfaces, merge getter/setter
    /// accessors so the generated class satisfies all interfaces.
    /// </summary>
    private static void MergePropertyAccessors(List<MockMemberModel> properties, int existingIndex,
        IPropertySymbol newProperty, ref int memberIdCounter)
    {
        var existing = properties[existingIndex];
        var needsGetter = !existing.HasGetter && newProperty.GetMethod is not null;
        var needsSetter = !existing.HasSetter && newProperty.SetMethod is not null;

        if (!needsGetter && !needsSetter) return;

        properties[existingIndex] = existing with
        {
            HasGetter = existing.HasGetter || newProperty.GetMethod is not null,
            HasSetter = existing.HasSetter || newProperty.SetMethod is not null,
            SetterMemberId = existing.HasSetter ? existing.SetterMemberId
                : newProperty.SetMethod is not null ? memberIdCounter++ : existing.SetterMemberId
        };
    }

    private static MockMemberModel CreatePropertyModel(IPropertySymbol property, ref int memberIdCounter, string? explicitInterfaceName, string? declaringInterfaceName = null)
    {
        var getterId = memberIdCounter++;
        var setterId = property.SetMethod is not null ? memberIdCounter++ : 0;

        return new MockMemberModel
        {
            Name = property.Name,
            MemberId = getterId,
            ReturnType = property.Type.GetFullyQualifiedNameWithNullability(),
            UnwrappedReturnType = property.Type.GetFullyQualifiedNameWithNullability(),
            IsVoid = false,
            IsAsync = false,
            IsProperty = true,
            HasGetter = property.GetMethod is not null,
            HasSetter = property.SetMethod is not null,
            SetterMemberId = setterId,
            ExplicitInterfaceName = explicitInterfaceName,
            DeclaringInterfaceName = declaringInterfaceName,
            NullableAnnotation = property.Type.NullableAnnotation.ToString(),
            SmartDefault = property.Type.GetSmartDefault(property.Type.IsNullableAnnotated()),
            IsAbstractMember = property.IsAbstract,
            IsVirtualMember = property.IsVirtual || property.IsOverride,
            IsProtected = property.DeclaredAccessibility == Accessibility.Protected
                       || property.DeclaredAccessibility == Accessibility.ProtectedOrInternal,
            IsRefStructReturn = property.Type.IsRefLikeType,
            IsReturnTypeStaticAbstractInterface = IsInterfaceWithStaticAbstractMembers(property.Type),
            SpanReturnElementType = property.Type.IsRefLikeType ? GetSpanElementType(property.Type) : null
        };
    }

    /// <summary>
    /// Discovers all accessible constructors of a class type for partial mock generation.
    /// </summary>
    public static EquatableArray<MockConstructorModel> DiscoverConstructors(INamedTypeSymbol typeSymbol, IAssemblySymbol? compilationAssembly = null)
    {
        var constructors = new List<MockConstructorModel>();

        foreach (var ctor in typeSymbol.InstanceConstructors)
        {
            if (ctor.DeclaredAccessibility == Accessibility.Private) continue;
            if (!IsMemberAccessible(ctor, compilationAssembly)) continue;

            constructors.Add(new MockConstructorModel
            {
                Parameters = new EquatableArray<MockParameterModel>(
                    ctor.Parameters.Select(p => new MockParameterModel
                    {
                        Name = EscapeIdentifier(p.Name),
                        Type = p.Type.GetMinimallyQualifiedNameWithNullability(),
                        FullyQualifiedType = p.Type.GetFullyQualifiedNameWithNullability(),
                        Direction = p.GetParameterDirection(),
                        HasDefaultValue = p.HasExplicitDefaultValue,
                        DefaultValueExpression = p.HasExplicitDefaultValue ? FormatDefaultValue(p) : null,
                        IsValueType = p.Type.IsValueType,
                        IsRefStruct = p.Type.IsRefLikeType
                    }).ToImmutableArray()
                )
            });
        }

        return new EquatableArray<MockConstructorModel>(constructors.ToImmutableArray());
    }

    private static MockMemberModel CreateIndexerModel(IPropertySymbol indexer, ref int memberIdCounter, string? explicitInterfaceName, string? declaringInterfaceName = null)
    {
        var getterId = memberIdCounter++;
        var setterId = indexer.SetMethod is not null ? memberIdCounter++ : 0;

        return new MockMemberModel
        {
            Name = "this",
            MemberId = getterId,
            SetterMemberId = setterId,
            ReturnType = indexer.Type.GetFullyQualifiedNameWithNullability(),
            UnwrappedReturnType = indexer.Type.GetFullyQualifiedNameWithNullability(),
            IsVoid = false,
            IsAsync = false,
            IsProperty = true,
            IsIndexer = true,
            HasGetter = indexer.GetMethod is not null,
            HasSetter = indexer.SetMethod is not null,
            Parameters = new EquatableArray<MockParameterModel>(
                indexer.Parameters.Select(p => new MockParameterModel
                {
                    Name = EscapeIdentifier(p.Name),
                    Type = p.Type.GetMinimallyQualifiedNameWithNullability(),
                    FullyQualifiedType = p.Type.GetFullyQualifiedNameWithNullability(),
                    Direction = ParameterDirection.In
                }).ToImmutableArray()
            ),
            ExplicitInterfaceName = explicitInterfaceName,
            DeclaringInterfaceName = declaringInterfaceName,
            NullableAnnotation = indexer.Type.NullableAnnotation.ToString(),
            SmartDefault = indexer.Type.GetSmartDefault(indexer.Type.IsNullableAnnotated())
        };
    }

    private static MockEventModel CreateEventModel(IEventSymbol evt, string? explicitInterfaceName, string? declaringInterfaceName = null)
    {
        var eventHandlerType = evt.Type.GetFullyQualifiedName();

        // Determine if this is an EventHandler pattern (sender + args)
        var isEventHandlerPattern = IsEventHandlerType(evt.Type);

        // Get the delegate's Invoke method to inspect parameters
        var invokeMethod = (evt.Type as INamedTypeSymbol)?.DelegateInvokeMethod;

        string raiseParameters;
        string invokeArgs;
        string eventArgsType;
        IParameterSymbol[] raiseParams;

        if (invokeMethod is null || invokeMethod.Parameters.Length == 0)
        {
            // Parameterless delegate (e.g., Action)
            raiseParameters = "";
            invokeArgs = "";
            eventArgsType = "";
            raiseParams = [];
        }
        else if (isEventHandlerPattern && invokeMethod.Parameters.Length >= 2)
        {
            // EventHandler pattern: skip sender (first param), expose remaining as raise params
            var argsParams = invokeMethod.Parameters.Skip(1).ToArray();
            raiseParameters = string.Join(", ", argsParams.Select(p => $"{p.Type.GetFullyQualifiedName()} {EscapeIdentifier(p.Name)}"));
            invokeArgs = "this, " + string.Join(", ", argsParams.Select(p => EscapeIdentifier(p.Name)));
            eventArgsType = argsParams.Length == 1
                ? argsParams[0].Type.GetFullyQualifiedName()
                : raiseParameters; // fallback for multi-arg EventHandler subtypes
            raiseParams = argsParams;
        }
        else
        {
            // Custom delegate (Action<T>, Func<T>, user-defined): expose all params
            raiseParameters = string.Join(", ", invokeMethod.Parameters.Select(p => $"{p.Type.GetFullyQualifiedName()} {EscapeIdentifier(p.Name)}"));
            invokeArgs = string.Join(", ", invokeMethod.Parameters.Select(p => EscapeIdentifier(p.Name)));
            eventArgsType = raiseParameters;
            raiseParams = invokeMethod.Parameters.ToArray();
        }

        var raiseParameterList = new EquatableArray<MockParameterModel>(
            raiseParams.Select(p => new MockParameterModel
            {
                Name = EscapeIdentifier(p.Name),
                FullyQualifiedType = p.Type.GetFullyQualifiedNameWithNullability(),
                Type = p.Type.GetMinimallyQualifiedNameWithNullability(),
                Direction = ParameterDirection.In
            }).ToImmutableArray());

        return new MockEventModel
        {
            Name = evt.Name,
            EventHandlerType = eventHandlerType,
            InvokeArgs = invokeArgs,
            EventArgsType = eventArgsType,
            ExplicitInterfaceName = explicitInterfaceName,
            DeclaringInterfaceName = declaringInterfaceName,
            RaiseParameterList = raiseParameterList
        };
    }

    private static bool IsEventHandlerType(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType)
        {
            return false;
        }

        // Check if the type is System.EventHandler or System.EventHandler<T>
        var fullName = namedType.ConstructedFrom.ToDisplayString();
        return fullName == "System.EventHandler" || fullName == "System.EventHandler<TEventArgs>";
    }

    private static string ComputeUnwrappedSmartDefault(ITypeSymbol returnType, bool isVoid, bool isAsync)
    {
        if (isVoid) return "";
        if (!isAsync) return returnType.GetSmartDefault(returnType.IsNullableAnnotated());

        // For async generic types (Task<T>/ValueTask<T>), get the inner type's smart default
        var innerType = returnType.GetAsyncInnerTypeSymbol();
        if (innerType is not null)
        {
            return innerType.GetSmartDefault(innerType.IsNullableAnnotated());
        }

        // Fallback (shouldn't happen for async non-void)
        return "default!";
    }

    private static string GetMethodKey(IMethodSymbol method)
    {
        var paramTypes = string.Join(',', method.Parameters.Select(p =>
            p.Type.GetFullyQualifiedName() + (p.RefKind != RefKind.None ? "&" : "")));
        var typeParams = method.TypeParameters.Length > 0 ? $"`{method.TypeParameters.Length}" : "";
        return $"M:{method.Name}{typeParams}({paramTypes})";
    }

    private static string? FormatDefaultValue(IParameterSymbol param)
    {
        if (!param.HasExplicitDefaultValue) return null;
        var value = param.ExplicitDefaultValue;
        if (value is null) return "default";
        if (value is string s) return $"\"{s}\"";
        if (value is bool b) return b ? "true" : "false";
        if (value is char c) return $"'{c}'";
        return value.ToString();
    }

    /// <summary>
    /// Escapes a parameter name that is a C# reserved keyword by prepending '@'.
    /// E.g., "event" → "@event", "class" → "@class", "return" → "@return".
    /// </summary>
    private static string EscapeIdentifier(string name) =>
        SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;

    /// <summary>
    /// For ReadOnlySpan&lt;T&gt; or Span&lt;T&gt; types, returns the fully qualified element type.
    /// Returns null for all other types.
    /// </summary>
    private static string? GetSpanElementType(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } namedType)
        {
            return null;
        }

        var constructed = namedType.ConstructedFrom;
        var ns = constructed.ContainingNamespace?.ToDisplayString();
        var name = constructed.MetadataName;

        if (ns == "System" && name is "ReadOnlySpan`1" or "Span`1")
        {
            return namedType.TypeArguments[0].GetFullyQualifiedName();
        }

        return null;
    }

    /// <summary>
    /// Returns true when the given type symbol is an interface that contains static abstract members
    /// (directly or via inherited interfaces) without a most specific implementation.
    /// Such types cannot be used as generic type arguments (CS8920).
    /// </summary>
    private static bool IsInterfaceWithStaticAbstractMembers(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol { TypeKind: TypeKind.Interface } namedType)
        {
            return false;
        }

        // Check both direct members and inherited interface members.
        // AllInterfaces does NOT include the type itself, so we check both.
        return namedType.GetMembers()
            .Concat(namedType.AllInterfaces.SelectMany(i => i.GetMembers()))
            .Any(m => m.IsStatic && m.IsAbstract);
    }

    /// <summary>
    /// Collects a static abstract interface member as a full MockMemberModel with IsStaticAbstract=true.
    /// Uses the existing CreateMethodModel/CreatePropertyModel to generate models with MemberIds,
    /// enabling full setup/verify support through the mock engine.
    /// </summary>
    private static void CollectStaticAbstractMember(
        ISymbol member,
        string interfaceFqn,
        List<MockMemberModel> methods,
        List<MockMemberModel> properties,
        List<MockEventModel> events,
        HashSet<string> seenMethods,
        Dictionary<string, int?> seenProperties,
        HashSet<string> seenEvents,
        ref int memberIdCounter)
    {
        switch (member)
        {
            case IMethodSymbol method when method.MethodKind == MethodKind.Ordinary:
            {
                var key = GetMethodKey(method);
                if (!seenMethods.Add(key)) break;

                var model = CreateMethodModel(method, ref memberIdCounter, interfaceFqn) with
                {
                    IsStaticAbstract = true
                };
                methods.Add(model);
                break;
            }

            case IPropertySymbol property when !property.IsIndexer:
            {
                var key = $"P:{property.Name}";
                if (seenProperties.ContainsKey(key)) break;

                seenProperties[key] = properties.Count;
                var model = CreatePropertyModel(property, ref memberIdCounter, interfaceFqn) with
                {
                    IsStaticAbstract = true
                };
                properties.Add(model);
                break;
            }

            case IEventSymbol evt:
            {
                var key = $"E:{evt.Name}";
                if (!seenEvents.Add(key)) break;

                var model = CreateEventModel(evt, interfaceFqn) with
                {
                    IsStaticAbstract = true
                };
                events.Add(model);
                break;
            }
        }
    }
}
