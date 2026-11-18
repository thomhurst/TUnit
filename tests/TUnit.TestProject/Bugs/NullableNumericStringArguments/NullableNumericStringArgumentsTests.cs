#if NET
using TUnit.TestProject.Attributes;
using TUnit.TestProject.Library.Bugs.NullableNumericStringArguments;

namespace TUnit.TestProject.Bugs.NullableNumericStringArguments;

[EngineTest(ExpectedResult.Pass)]
[InheritsTests]
public class NullableNumericStringArgumentsTests : NullableNumericStringArgumentsBase;
#endif
