// ReSharper disable once CheckNamespace
#if NETFRAMEWORK
namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
internal sealed class ModuleInitializerAttribute : Attribute
{
}
#endif
