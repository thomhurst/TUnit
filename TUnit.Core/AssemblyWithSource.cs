using System.Reflection;

namespace TUnit.Core;

internal record AssemblyWithSource(string Source, string RootedSource, Assembly Assembly);