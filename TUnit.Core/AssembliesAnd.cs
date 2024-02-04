using System.Reflection;

namespace TUnit.Core;

public record AssembliesAnd<T>(Assembly[] Assemblies, IEnumerable<T> Values);