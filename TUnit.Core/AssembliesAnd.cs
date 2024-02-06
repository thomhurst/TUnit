namespace TUnit.Core;

internal record AssembliesAnd<T>(AssemblyWithSource[] Assemblies, IEnumerable<T> Values);