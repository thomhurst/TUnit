// Two types with the same simple name in different namespaces. Both instantiations of
// GenericMethodNames.SameSimpleName<T> are displayed as SameSimpleName<SamePayload>, so their
// test ids must still differ (#7105).

namespace TUnit.TestProject.Bugs._7105.First
{
    public class SamePayload;
}

namespace TUnit.TestProject.Bugs._7105.Second
{
    public class SamePayload;
}
