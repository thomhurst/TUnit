using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Type type using [AssertionFrom&lt;Type&gt;] attributes.
/// Each assertion wraps a property from the Type class.
/// </summary>
[AssertionFrom<Type>(nameof(Type.IsClass), ExpectationMessage = "be a class")]
[AssertionFrom<Type>(nameof(Type.IsClass), CustomName = "IsNotClass", NegateLogic = true, ExpectationMessage = "be a class")]

[AssertionFrom<Type>(nameof(Type.IsInterface), ExpectationMessage = "be an interface")]
[AssertionFrom<Type>(nameof(Type.IsInterface), CustomName = "IsNotInterface", NegateLogic = true, ExpectationMessage = "be an interface")]

[AssertionFrom<Type>(nameof(Type.IsAbstract), ExpectationMessage = "be abstract")]
[AssertionFrom<Type>(nameof(Type.IsAbstract), CustomName = "IsNotAbstract", NegateLogic = true, ExpectationMessage = "be abstract")]

[AssertionFrom<Type>(nameof(Type.IsSealed), ExpectationMessage = "be sealed")]
[AssertionFrom<Type>(nameof(Type.IsSealed), CustomName = "IsNotSealed", NegateLogic = true, ExpectationMessage = "be sealed")]

[AssertionFrom<Type>(nameof(Type.IsValueType), ExpectationMessage = "be a value type")]
[AssertionFrom<Type>(nameof(Type.IsValueType), CustomName = "IsNotValueType", NegateLogic = true, ExpectationMessage = "be a value type")]

[AssertionFrom<Type>(nameof(Type.IsEnum), ExpectationMessage = "be an enum")]
[AssertionFrom<Type>(nameof(Type.IsEnum), CustomName = "IsNotEnum", NegateLogic = true, ExpectationMessage = "be an enum")]

[AssertionFrom<Type>(nameof(Type.IsPrimitive), ExpectationMessage = "be a primitive type")]
[AssertionFrom<Type>(nameof(Type.IsPrimitive), CustomName = "IsNotPrimitive", NegateLogic = true, ExpectationMessage = "be a primitive type")]

[AssertionFrom<Type>(nameof(Type.IsPublic), ExpectationMessage = "be public")]
[AssertionFrom<Type>(nameof(Type.IsPublic), CustomName = "IsNotPublic", NegateLogic = true, ExpectationMessage = "be public")]

[AssertionFrom<Type>(nameof(Type.IsGenericType), ExpectationMessage = "be a generic type")]
[AssertionFrom<Type>(nameof(Type.IsGenericType), CustomName = "IsNotGenericType", NegateLogic = true, ExpectationMessage = "be a generic type")]

[AssertionFrom<Type>(nameof(Type.IsGenericTypeDefinition), ExpectationMessage = "be a generic type definition")]
[AssertionFrom<Type>(nameof(Type.IsGenericTypeDefinition), CustomName = "IsNotGenericTypeDefinition", NegateLogic = true, ExpectationMessage = "be a generic type definition")]

[AssertionFrom<Type>(nameof(Type.IsArray), ExpectationMessage = "be an array")]
[AssertionFrom<Type>(nameof(Type.IsArray), CustomName = "IsNotArray", NegateLogic = true, ExpectationMessage = "be an array")]

[AssertionFrom<Type>(nameof(Type.IsByRef), ExpectationMessage = "be a by-ref type")]
[AssertionFrom<Type>(nameof(Type.IsByRef), CustomName = "IsNotByRef", NegateLogic = true, ExpectationMessage = "be a by-ref type")]

#if NET5_0_OR_GREATER
[AssertionFrom<Type>(nameof(Type.IsByRefLike), ExpectationMessage = "be a by-ref-like type")]
[AssertionFrom<Type>(nameof(Type.IsByRefLike), CustomName = "IsNotByRefLike", NegateLogic = true, ExpectationMessage = "be a by-ref-like type")]
#endif

[AssertionFrom<Type>(nameof(Type.IsPointer), ExpectationMessage = "be a pointer type")]
[AssertionFrom<Type>(nameof(Type.IsPointer), CustomName = "IsNotPointer", NegateLogic = true, ExpectationMessage = "be a pointer type")]

[AssertionFrom<Type>(nameof(Type.IsNested), ExpectationMessage = "be a nested type")]
[AssertionFrom<Type>(nameof(Type.IsNested), CustomName = "IsNotNested", NegateLogic = true, ExpectationMessage = "be a nested type")]

[AssertionFrom<Type>(nameof(Type.IsNestedPublic), ExpectationMessage = "be a nested public type")]
[AssertionFrom<Type>(nameof(Type.IsNestedPublic), CustomName = "IsNotNestedPublic", NegateLogic = true, ExpectationMessage = "be a nested public type")]

[AssertionFrom<Type>(nameof(Type.IsNestedPrivate), ExpectationMessage = "be a nested private type")]
[AssertionFrom<Type>(nameof(Type.IsNestedPrivate), CustomName = "IsNotNestedPrivate", NegateLogic = true, ExpectationMessage = "be a nested private type")]

[AssertionFrom<Type>(nameof(Type.IsNestedAssembly), ExpectationMessage = "be a nested assembly type")]
[AssertionFrom<Type>(nameof(Type.IsNestedAssembly), CustomName = "IsNotNestedAssembly", NegateLogic = true, ExpectationMessage = "be a nested assembly type")]

[AssertionFrom<Type>(nameof(Type.IsNestedFamily), ExpectationMessage = "be a nested family type")]
[AssertionFrom<Type>(nameof(Type.IsNestedFamily), CustomName = "IsNotNestedFamily", NegateLogic = true, ExpectationMessage = "be a nested family type")]

[AssertionFrom<Type>(nameof(Type.IsVisible), ExpectationMessage = "be visible")]
[AssertionFrom<Type>(nameof(Type.IsVisible), CustomName = "IsNotVisible", NegateLogic = true, ExpectationMessage = "be visible")]

[AssertionFrom<Type>(nameof(Type.IsConstructedGenericType), ExpectationMessage = "be a constructed generic type")]
[AssertionFrom<Type>(nameof(Type.IsConstructedGenericType), CustomName = "IsNotConstructedGenericType", NegateLogic = true, ExpectationMessage = "be a constructed generic type")]

[AssertionFrom<Type>(nameof(Type.ContainsGenericParameters), ExpectationMessage = "contain generic parameters")]
[AssertionFrom<Type>(nameof(Type.ContainsGenericParameters), CustomName = "DoesNotContainGenericParameters", NegateLogic = true, ExpectationMessage = "contain generic parameters")]

#if !NET5_0_OR_GREATER
[AssertionFrom<Type>(nameof(Type.IsSerializable), ExpectationMessage = "be serializable")]
[AssertionFrom<Type>(nameof(Type.IsSerializable), CustomName = "IsNotSerializable", NegateLogic = true, ExpectationMessage = "be serializable")]
#endif

[AssertionFrom<Type>(nameof(Type.IsCOMObject), ExpectationMessage = "be a COM object")]
[AssertionFrom<Type>(nameof(Type.IsCOMObject), CustomName = "IsNotCOMObject", NegateLogic = true, ExpectationMessage = "be a COM object")]
public static partial class TypeAssertionExtensions
{
}
