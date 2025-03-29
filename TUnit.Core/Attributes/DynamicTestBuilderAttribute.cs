using System.Runtime.CompilerServices;

namespace TUnit.Core;

public class DynamicTestBuilderAttribute([CallerFilePath] string file = "", [CallerLineNumber] int line = 0) : BaseTestAttribute(file, line);