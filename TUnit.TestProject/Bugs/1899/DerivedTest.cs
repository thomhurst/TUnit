using TUnit.TestProject.Attributes;
using TUnit.TestProject.Library.Bugs._1899;

namespace TUnit.TestProject.Bugs._1899;

[EngineTest(ExpectedResult.Pass)]
[InheritsTests]
public class DerivedTest : BaseClass<DummyReferenceTypeClass>;
