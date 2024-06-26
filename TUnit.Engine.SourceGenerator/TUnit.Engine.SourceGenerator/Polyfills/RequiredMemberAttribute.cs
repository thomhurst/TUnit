// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

#pragma warning disable
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
internal sealed class RequiredMemberAttribute : Attribute;