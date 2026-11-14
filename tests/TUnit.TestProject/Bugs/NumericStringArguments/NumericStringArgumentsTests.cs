#if NET
using TUnit.TestProject.Attributes;
using TUnit.TestProject.Library.Bugs.NumericStringArguments;

namespace TUnit.TestProject.Bugs.NumericStringArguments;

[EngineTest(ExpectedResult.Pass)]
[InheritsTests]
public class NumericStringArgumentsTests : NumericStringArgumentsBase;
#endif
