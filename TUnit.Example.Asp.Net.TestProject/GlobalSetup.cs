// Here you could define global logic that would affect all tests

// You can use attributes at the assembly level to apply to all tests in the assembly

using TUnit.Core.Enums;

[assembly: System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[assembly: RunOn(OS.Linux)]
