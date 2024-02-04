using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using TUnit.TestAdapter.Constants;

namespace TUnit.TestAdapter;

public class TUnitProperties
{
    internal static readonly TestProperty TestCategory = TestProperty.Register(
        id: TestAdapterConstants.TestCategory,
        label: "TestCategory",
        valueType: typeof(string[]),
        TestPropertyAttributes.Hidden,
        owner: typeof(TestCase));
}