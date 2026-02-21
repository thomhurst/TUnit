using Microsoft.CodeAnalysis;
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
        DiscoverMembers(ITypeSymbol typeSymbol)
    {
        var methods = new List<MockMemberModel>();
        var properties = new List<MockMemberModel>();
        var events = new List<MockEventModel>();

        var seenMethods = new HashSet<string>();
        var seenProperties = new Dictionary<string, int>();
        var seenEvents = new HashSet<string>();

        int memberIdCounter = 0;

        // Collect all interfaces to scan
        var interfaces = typeSymbol.TypeKind == TypeKind.Interface
            ? new[] { typeSymbol }.Concat(typeSymbol.AllInterfaces)
            : typeSymbol.AllInterfaces.AsEnumerable();

        // If it's a class, also include its own members
        if (typeSymbol.TypeKind == TypeKind.Class)
        {
            ProcessClassMembers(typeSymbol, methods, properties, events,
                seenMethods, seenProperties, seenEvents, ref memberIdCounter);
        }

        foreach (var iface in interfaces)
        {
            string? explicitInterfaceName = null;

            foreach (var member in iface.GetMembers())
            {
                if (member.IsStatic) continue;

                switch (member)
                {
                    case IMethodSymbol method when method.MethodKind == MethodKind.Ordinary:
                    {
                        var key = GetMethodKey(method);
                        if (!seenMethods.Add(key)) continue;

                        methods.Add(CreateMethodModel(method, ref memberIdCounter, explicitInterfaceName));
                        break;
                    }

                    case IPropertySymbol property when !property.IsIndexer:
                    {
                        var key = $"P:{property.Name}";
                        if (seenProperties.TryGetValue(key, out var existingIndex))
                        {
                            MergePropertyAccessors(properties, existingIndex, property, ref memberIdCounter);
                        }
                        else
                        {
                            seenProperties[key] = properties.Count;
                            properties.Add(CreatePropertyModel(property, ref memberIdCounter, explicitInterfaceName));
                        }
                        break;
                    }

                    case IPropertySymbol indexer when indexer.IsIndexer:
                    {
                        var paramTypes = string.Join(",", indexer.Parameters.Select(p => p.Type.GetFullyQualifiedName()));
                        var key = $"I:[{paramTypes}]";
                        if (seenProperties.TryGetValue(key, out var existingIndex))
                        {
                            MergePropertyAccessors(properties, existingIndex, indexer, ref memberIdCounter);
                        }
                        else
                        {
                            seenProperties[key] = properties.Count;
                            properties.Add(CreateIndexerModel(indexer, ref memberIdCounter, explicitInterfaceName));
                        }
                        break;
                    }

                    case IEventSymbol evt:
                    {
                        var key = $"E:{evt.Name}";
                        if (!seenEvents.Add(key)) continue;

                        events.Add(CreateEventModel(evt, explicitInterfaceName));
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
        DiscoverMembersFromMultipleTypes(INamedTypeSymbol[] typeSymbols)
    {
        var methods = new List<MockMemberModel>();
        var properties = new List<MockMemberModel>();
        var events = new List<MockEventModel>();

        var seenMethods = new HashSet<string>();
        var seenProperties = new Dictionary<string, int>();
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
                foreach (var member in iface.GetMembers())
                {
                    if (member.IsStatic) continue;

                    switch (member)
                    {
                        case IMethodSymbol method when method.MethodKind == MethodKind.Ordinary:
                        {
                            var key = GetMethodKey(method);
                            if (!seenMethods.Add(key)) continue;
                            methods.Add(CreateMethodModel(method, ref memberIdCounter, null));
                            break;
                        }

                        case IPropertySymbol property when !property.IsIndexer:
                        {
                            var key = $"P:{property.Name}";
                            if (seenProperties.TryGetValue(key, out var existingIndex))
                            {
                                MergePropertyAccessors(properties, existingIndex, property, ref memberIdCounter);
                            }
                            else
                            {
                                seenProperties[key] = properties.Count;
                                properties.Add(CreatePropertyModel(property, ref memberIdCounter, null));
                            }
                            break;
                        }

                        case IPropertySymbol indexer when indexer.IsIndexer:
                        {
                            var paramTypes = string.Join(",", indexer.Parameters.Select(p => p.Type.GetFullyQualifiedName()));
                            var key = $"I:[{paramTypes}]";
                            if (seenProperties.TryGetValue(key, out var existingIndex))
                            {
                                MergePropertyAccessors(properties, existingIndex, indexer, ref memberIdCounter);
                            }
                            else
                            {
                                seenProperties[key] = properties.Count;
                                properties.Add(CreateIndexerModel(indexer, ref memberIdCounter, null));
                            }
                            break;
                        }

                        case IEventSymbol evt:
                        {
                            var key = $"E:{evt.Name}";
                            if (!seenEvents.Add(key)) continue;
                            events.Add(CreateEventModel(evt, null));
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
        List<MockMemberModel> methods,
        List<MockMemberModel> properties,
        List<MockEventModel> events,
        HashSet<string> seenMethods,
        Dictionary<string, int> seenProperties,
        HashSet<string> seenEvents,
        ref int memberIdCounter)
    {
        // Walk up the class hierarchy
        var current = typeSymbol;
        while (current != null && current.SpecialType != SpecialType.System_Object)
        {
            foreach (var member in current.GetMembers())
            {
                if (member.IsStatic) continue;

                // Skip private members and sealed overrides (can't override them)
                if (member.DeclaredAccessibility == Accessibility.Private) continue;
                if (member is IMethodSymbol { IsSealed: true }) continue;
                if (member is IPropertySymbol { IsSealed: true }) continue;

                switch (member)
                {
                    case IMethodSymbol method when method.MethodKind == MethodKind.Ordinary
                        && (method.IsAbstract || method.IsVirtual || method.IsOverride):
                    {
                        var key = GetMethodKey(method);
                        if (!seenMethods.Add(key)) continue;

                        methods.Add(CreateMethodModel(method, ref memberIdCounter, null));
                        break;
                    }

                    case IPropertySymbol property when !property.IsIndexer
                        && (property.IsAbstract || property.IsVirtual || property.IsOverride):
                    {
                        var key = $"P:{property.Name}";
                        if (seenProperties.TryGetValue(key, out var existingIndex))
                        {
                            MergePropertyAccessors(properties, existingIndex, property, ref memberIdCounter);
                        }
                        else
                        {
                            seenProperties[key] = properties.Count;
                            properties.Add(CreatePropertyModel(property, ref memberIdCounter, null));
                        }
                        break;
                    }

                    case IEventSymbol evt when evt.IsAbstract || evt.IsVirtual || evt.IsOverride:
                    {
                        var key = $"E:{evt.Name}";
                        if (!seenEvents.Add(key)) continue;

                        events.Add(CreateEventModel(evt, null));
                        break;
                    }
                }
            }

            current = current.BaseType;
        }
    }

    /// <summary>
    /// Creates a MockMemberModel from a delegate type's Invoke method.
    /// </summary>
    public static MockMemberModel CreateDelegateInvokeModel(IMethodSymbol invokeMethod, ref int memberIdCounter)
    {
        return CreateMethodModel(invokeMethod, ref memberIdCounter, null);
    }

    private static MockMemberModel CreateMethodModel(IMethodSymbol method, ref int memberIdCounter, string? explicitInterfaceName)
    {
        var returnType = method.ReturnType;
        var isAsync = returnType.IsAsyncReturnType();
        var isValueTask = returnType.IsValueTaskReturnType();
        var (unwrappedType, isVoidAsync) = returnType.GetUnwrappedReturnType();
        var isVoid = method.ReturnsVoid || isVoidAsync;

        return new MockMemberModel
        {
            Name = method.Name,
            MemberId = memberIdCounter++,
            ReturnType = returnType.GetFullyQualifiedName(),
            UnwrappedReturnType = unwrappedType,
            IsVoid = isVoid,
            IsAsync = isAsync,
            IsValueTask = isValueTask,
            IsProperty = false,
            IsGenericMethod = method.IsGenericMethod,
            Parameters = new EquatableArray<MockParameterModel>(
                method.Parameters.Select(p => new MockParameterModel
                {
                    Name = p.Name,
                    Type = p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    FullyQualifiedType = p.Type.GetFullyQualifiedName(),
                    Direction = p.GetParameterDirection(),
                    HasDefaultValue = p.HasExplicitDefaultValue,
                    DefaultValueExpression = p.HasExplicitDefaultValue ? FormatDefaultValue(p) : null
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
            NullableAnnotation = returnType.NullableAnnotation.ToString(),
            SmartDefault = isVoid ? "" : returnType.GetSmartDefault(returnType.IsNullableAnnotated()),
            UnwrappedSmartDefault = ComputeUnwrappedSmartDefault(returnType, isVoid, isAsync),
            IsAbstractMember = method.IsAbstract,
            IsVirtualMember = method.IsVirtual || method.IsOverride,
            IsProtected = method.DeclaredAccessibility == Accessibility.Protected
                       || method.DeclaredAccessibility == Accessibility.ProtectedOrInternal
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

    private static MockMemberModel CreatePropertyModel(IPropertySymbol property, ref int memberIdCounter, string? explicitInterfaceName)
    {
        var getterId = memberIdCounter++;
        var setterId = property.SetMethod is not null ? memberIdCounter++ : 0;

        return new MockMemberModel
        {
            Name = property.Name,
            MemberId = getterId,
            ReturnType = property.Type.GetFullyQualifiedName(),
            UnwrappedReturnType = property.Type.GetFullyQualifiedName(),
            IsVoid = false,
            IsAsync = false,
            IsProperty = true,
            HasGetter = property.GetMethod is not null,
            HasSetter = property.SetMethod is not null,
            SetterMemberId = setterId,
            ExplicitInterfaceName = explicitInterfaceName,
            NullableAnnotation = property.Type.NullableAnnotation.ToString(),
            SmartDefault = property.Type.GetSmartDefault(property.Type.IsNullableAnnotated()),
            IsAbstractMember = property.IsAbstract,
            IsVirtualMember = property.IsVirtual || property.IsOverride,
            IsProtected = property.DeclaredAccessibility == Accessibility.Protected
                       || property.DeclaredAccessibility == Accessibility.ProtectedOrInternal
        };
    }

    /// <summary>
    /// Discovers all accessible constructors of a class type for partial mock generation.
    /// </summary>
    public static EquatableArray<MockConstructorModel> DiscoverConstructors(INamedTypeSymbol typeSymbol)
    {
        var constructors = new List<MockConstructorModel>();

        foreach (var ctor in typeSymbol.InstanceConstructors)
        {
            if (ctor.DeclaredAccessibility == Accessibility.Private) continue;

            constructors.Add(new MockConstructorModel
            {
                Parameters = new EquatableArray<MockParameterModel>(
                    ctor.Parameters.Select(p => new MockParameterModel
                    {
                        Name = p.Name,
                        Type = p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                        FullyQualifiedType = p.Type.GetFullyQualifiedName(),
                        Direction = p.GetParameterDirection(),
                        HasDefaultValue = p.HasExplicitDefaultValue,
                        DefaultValueExpression = p.HasExplicitDefaultValue ? FormatDefaultValue(p) : null
                    }).ToImmutableArray()
                )
            });
        }

        return new EquatableArray<MockConstructorModel>(constructors.ToImmutableArray());
    }

    private static MockMemberModel CreateIndexerModel(IPropertySymbol indexer, ref int memberIdCounter, string? explicitInterfaceName)
    {
        var getterId = memberIdCounter++;
        var setterId = indexer.SetMethod is not null ? memberIdCounter++ : 0;

        return new MockMemberModel
        {
            Name = "this",
            MemberId = getterId,
            SetterMemberId = setterId,
            ReturnType = indexer.Type.GetFullyQualifiedName(),
            UnwrappedReturnType = indexer.Type.GetFullyQualifiedName(),
            IsVoid = false,
            IsAsync = false,
            IsProperty = true,
            IsIndexer = true,
            HasGetter = indexer.GetMethod is not null,
            HasSetter = indexer.SetMethod is not null,
            Parameters = new EquatableArray<MockParameterModel>(
                indexer.Parameters.Select(p => new MockParameterModel
                {
                    Name = p.Name,
                    Type = p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    FullyQualifiedType = p.Type.GetFullyQualifiedName(),
                    Direction = ParameterDirection.In
                }).ToImmutableArray()
            ),
            ExplicitInterfaceName = explicitInterfaceName,
            NullableAnnotation = indexer.Type.NullableAnnotation.ToString(),
            SmartDefault = indexer.Type.GetSmartDefault(indexer.Type.IsNullableAnnotated())
        };
    }

    private static MockEventModel CreateEventModel(IEventSymbol evt, string? explicitInterfaceName)
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
            raiseParameters = string.Join(", ", argsParams.Select(p => $"{p.Type.GetFullyQualifiedName()} {p.Name}"));
            invokeArgs = "this, " + string.Join(", ", argsParams.Select(p => p.Name));
            eventArgsType = argsParams.Length == 1
                ? argsParams[0].Type.GetFullyQualifiedName()
                : raiseParameters; // fallback for multi-arg EventHandler subtypes
            raiseParams = argsParams;
        }
        else
        {
            // Custom delegate (Action<T>, Func<T>, user-defined): expose all params
            raiseParameters = string.Join(", ", invokeMethod.Parameters.Select(p => $"{p.Type.GetFullyQualifiedName()} {p.Name}"));
            invokeArgs = string.Join(", ", invokeMethod.Parameters.Select(p => p.Name));
            eventArgsType = raiseParameters;
            raiseParams = invokeMethod.Parameters.ToArray();
        }

        var raiseParameterList = new EquatableArray<MockParameterModel>(
            raiseParams.Select(p => new MockParameterModel
            {
                Name = p.Name,
                FullyQualifiedType = p.Type.GetFullyQualifiedName(),
                Type = p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                Direction = ParameterDirection.In
            }).ToImmutableArray());

        return new MockEventModel
        {
            Name = evt.Name,
            EventHandlerType = eventHandlerType,
            InvokeArgs = invokeArgs,
            EventArgsType = eventArgsType,
            ExplicitInterfaceName = explicitInterfaceName,
            RaiseParameterList = raiseParameterList
        };
    }

    private static bool IsEventHandlerType(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType)
            return false;

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
        var paramTypes = string.Join(",", method.Parameters.Select(p =>
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
}
