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
public static partial class TypeAssertionExtensions
{
}
