using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace TUnit.Core;

/// <summary>
/// Marks a method as a dynamic test builder that programmatically generates test cases at runtime.
/// Methods decorated with this attribute can yield test definitions dynamically.
/// </summary>
public class DynamicTestBuilderAttribute([CallerFilePath] string file = "", [CallerLineNumber] int line = 0) : BaseTestAttribute(file, line);
