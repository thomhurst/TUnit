using System;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

// Type reflection assertions
[CreateAssertion<Type>( nameof(Type.IsAbstract))]
[CreateAssertion<Type>( nameof(Type.IsAbstract), CustomName = "IsNotAbstract", NegateLogic = true)]

[CreateAssertion<Type>( nameof(Type.IsInterface))]
[CreateAssertion<Type>( nameof(Type.IsInterface), CustomName = "IsNotInterface", NegateLogic = true)]

[CreateAssertion<Type>( nameof(Type.IsClass))]
[CreateAssertion<Type>( nameof(Type.IsClass), CustomName = "IsNotClass", NegateLogic = true)]

[CreateAssertion<Type>( nameof(Type.IsValueType))]
[CreateAssertion<Type>( nameof(Type.IsValueType), CustomName = "IsReferenceType", NegateLogic = true)]

[CreateAssertion<Type>( nameof(Type.IsSealed))]
[CreateAssertion<Type>( nameof(Type.IsSealed), CustomName = "IsNotSealed", NegateLogic = true)]

[CreateAssertion<Type>( nameof(Type.IsPublic))]
[CreateAssertion<Type>( nameof(Type.IsPublic), CustomName = "IsNotPublic", NegateLogic = true)]

[CreateAssertion<Type>( nameof(Type.IsEnum))]
[CreateAssertion<Type>( nameof(Type.IsEnum), CustomName = "IsNotEnum", NegateLogic = true)]

[CreateAssertion<Type>( nameof(Type.IsGenericType))]
[CreateAssertion<Type>( nameof(Type.IsGenericType), CustomName = "IsNotGenericType", NegateLogic = true)]

[CreateAssertion<Type>( nameof(Type.IsGenericTypeDefinition))]
[CreateAssertion<Type>( nameof(Type.IsGenericTypeDefinition), CustomName = "IsNotGenericTypeDefinition", NegateLogic = true)]

[CreateAssertion<Type>( nameof(Type.IsArray))]
[CreateAssertion<Type>( nameof(Type.IsArray), CustomName = "IsNotArray", NegateLogic = true)]

[CreateAssertion<Type>( nameof(Type.IsPrimitive))]
[CreateAssertion<Type>( nameof(Type.IsPrimitive), CustomName = "IsNotPrimitive", NegateLogic = true)]

[CreateAssertion<Type>( nameof(Type.IsNested))]
[CreateAssertion<Type>( nameof(Type.IsNested), CustomName = "IsNotNested", NegateLogic = true)]

#if !NET
[CreateAssertion<Type>( nameof(Type.IsSerializable))]
[CreateAssertion<Type>( nameof(Type.IsSerializable), CustomName = "IsNotSerializable", NegateLogic = true)]
#endif

[CreateAssertion<Type>( nameof(Type.IsAssignableFrom))]
[CreateAssertion<Type>( nameof(Type.IsAssignableFrom), CustomName = "IsNotAssignableFrom", NegateLogic = true)]

#if NET5_0_OR_GREATER
[CreateAssertion<Type>( nameof(Type.IsAssignableTo))]
[CreateAssertion<Type>( nameof(Type.IsAssignableTo), CustomName = "IsNotAssignableTo", NegateLogic = true)]
#endif
public static partial class TypeAssertionExtensions;
