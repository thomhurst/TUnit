using System;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

// Type reflection assertions
[CreateAssertion(typeof(Type), nameof(Type.IsAbstract))]
[CreateAssertion(typeof(Type), nameof(Type.IsAbstract), CustomName = "IsNotAbstract", NegateLogic = true)]

[CreateAssertion(typeof(Type), nameof(Type.IsInterface))]
[CreateAssertion(typeof(Type), nameof(Type.IsInterface), CustomName = "IsNotInterface", NegateLogic = true)]

[CreateAssertion(typeof(Type), nameof(Type.IsClass))]
[CreateAssertion(typeof(Type), nameof(Type.IsClass), CustomName = "IsNotClass", NegateLogic = true)]

[CreateAssertion(typeof(Type), nameof(Type.IsValueType))]
[CreateAssertion(typeof(Type), nameof(Type.IsValueType), CustomName = "IsReferenceType", NegateLogic = true)]

[CreateAssertion(typeof(Type), nameof(Type.IsSealed))]
[CreateAssertion(typeof(Type), nameof(Type.IsSealed), CustomName = "IsNotSealed", NegateLogic = true)]

[CreateAssertion(typeof(Type), nameof(Type.IsPublic))]
[CreateAssertion(typeof(Type), nameof(Type.IsPublic), CustomName = "IsNotPublic", NegateLogic = true)]

[CreateAssertion(typeof(Type), nameof(Type.IsEnum))]
[CreateAssertion(typeof(Type), nameof(Type.IsEnum), CustomName = "IsNotEnum", NegateLogic = true)]

[CreateAssertion(typeof(Type), nameof(Type.IsGenericType))]
[CreateAssertion(typeof(Type), nameof(Type.IsGenericType), CustomName = "IsNotGenericType", NegateLogic = true)]

[CreateAssertion(typeof(Type), nameof(Type.IsGenericTypeDefinition))]
[CreateAssertion(typeof(Type), nameof(Type.IsGenericTypeDefinition), CustomName = "IsNotGenericTypeDefinition", NegateLogic = true)]

[CreateAssertion(typeof(Type), nameof(Type.IsArray))]
[CreateAssertion(typeof(Type), nameof(Type.IsArray), CustomName = "IsNotArray", NegateLogic = true)]

[CreateAssertion(typeof(Type), nameof(Type.IsPrimitive))]
[CreateAssertion(typeof(Type), nameof(Type.IsPrimitive), CustomName = "IsNotPrimitive", NegateLogic = true)]

[CreateAssertion(typeof(Type), nameof(Type.IsNested))]
[CreateAssertion(typeof(Type), nameof(Type.IsNested), CustomName = "IsNotNested", NegateLogic = true)]

#if !NET
[CreateAssertion(typeof(Type), nameof(Type.IsSerializable))]
[CreateAssertion(typeof(Type), nameof(Type.IsSerializable), CustomName = "IsNotSerializable", NegateLogic = true)]
#endif

[CreateAssertion(typeof(Type), nameof(Type.IsAssignableFrom))]
[CreateAssertion(typeof(Type), nameof(Type.IsAssignableFrom), CustomName = "IsNotAssignableFrom", NegateLogic = true)]

#if NET5_0_OR_GREATER
[CreateAssertion(typeof(Type), nameof(Type.IsAssignableTo))]
[CreateAssertion(typeof(Type), nameof(Type.IsAssignableTo), CustomName = "IsNotAssignableTo", NegateLogic = true)]
#endif
public static partial class TypeAssertionExtensions;
