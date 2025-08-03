using TUnit.TestProject.Attributes;
using TUnit.TestProject.Library.Bugs._1889;

namespace TUnit.TestProject.Bugs._1889;

[EngineTest(ExpectedResult.Pass)]
[InheritsTests]
public class DerivedTest : BaseTest<DummyReferenceTypeClass>;
