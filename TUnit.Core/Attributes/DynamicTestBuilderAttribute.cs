using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace TUnit.Core;

[Experimental("TUnitWIP0001")]
public class DynamicTestBuilderAttribute([CallerFilePath] string file = "", [CallerLineNumber] int line = 0) : BaseTestAttribute(file, line);
