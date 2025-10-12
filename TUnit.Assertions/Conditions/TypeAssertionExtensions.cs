using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Type type using [AssertionFrom&lt;Type&gt;] attributes.
/// Each assertion wraps a property from the Type class.
/// </summary>
[AssertionFrom<Type>("IsClass", ExpectationMessage = "be a class")]
[AssertionFrom<Type>("IsClass", CustomName = "IsNotClass", NegateLogic = true, ExpectationMessage = "be a class")]

[AssertionFrom<Type>("IsInterface", ExpectationMessage = "be an interface")]
[AssertionFrom<Type>("IsInterface", CustomName = "IsNotInterface", NegateLogic = true, ExpectationMessage = "be an interface")]

[AssertionFrom<Type>("IsAbstract", ExpectationMessage = "be abstract")]
[AssertionFrom<Type>("IsAbstract", CustomName = "IsNotAbstract", NegateLogic = true, ExpectationMessage = "be abstract")]

[AssertionFrom<Type>("IsSealed", ExpectationMessage = "be sealed")]
[AssertionFrom<Type>("IsSealed", CustomName = "IsNotSealed", NegateLogic = true, ExpectationMessage = "be sealed")]

[AssertionFrom<Type>("IsValueType", ExpectationMessage = "be a value type")]
[AssertionFrom<Type>("IsValueType", CustomName = "IsNotValueType", NegateLogic = true, ExpectationMessage = "be a value type")]

[AssertionFrom<Type>("IsEnum", ExpectationMessage = "be an enum")]
[AssertionFrom<Type>("IsEnum", CustomName = "IsNotEnum", NegateLogic = true, ExpectationMessage = "be an enum")]

[AssertionFrom<Type>("IsPrimitive", ExpectationMessage = "be a primitive type")]
[AssertionFrom<Type>("IsPrimitive", CustomName = "IsNotPrimitive", NegateLogic = true, ExpectationMessage = "be a primitive type")]

[AssertionFrom<Type>("IsPublic", ExpectationMessage = "be public")]
[AssertionFrom<Type>("IsPublic", CustomName = "IsNotPublic", NegateLogic = true, ExpectationMessage = "be public")]

[AssertionFrom<Type>("IsGenericType", ExpectationMessage = "be a generic type")]
[AssertionFrom<Type>("IsGenericType", CustomName = "IsNotGenericType", NegateLogic = true, ExpectationMessage = "be a generic type")]

[AssertionFrom<Type>("IsGenericTypeDefinition", ExpectationMessage = "be a generic type definition")]
[AssertionFrom<Type>("IsGenericTypeDefinition", CustomName = "IsNotGenericTypeDefinition", NegateLogic = true, ExpectationMessage = "be a generic type definition")]
public static partial class TypeAssertionExtensions
{
}
