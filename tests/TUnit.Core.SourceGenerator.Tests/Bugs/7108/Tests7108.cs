namespace TUnit.Core.SourceGenerator.Tests;

internal class Tests7108 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(
        Git.TestsDirectory.FullName,
        "TUnit.TestProject",
        "Bugs",
        "_7108",
        "GenericMethodNonTupleMethodDataSourceTests.cs"),
        _ => Task.CompletedTask);

    [Test]
    public Task Public_Object_Field() => RunTest(Path.Combine(
        Git.TestsDirectory.FullName,
        "TUnit.TestProject",
        "Bugs",
        "_7108",
        "PublicObjectFieldDataSourceTests.cs"),
        _ => Task.CompletedTask);

    [Test]
    public Task Private_Row_Type() => RunTest(Path.Combine(
        Git.TestsDirectory.FullName,
        "TUnit.TestProject",
        "Bugs",
        "_7108",
        "PrivateRowTypeDataSourceTests.cs"),
        _ => Task.CompletedTask);

    [Test]
    public Task Inherited_Protected_Member() => RunTest(Path.Combine(
        Git.TestsDirectory.FullName,
        "TUnit.TestProject",
        "Bugs",
        "_7108",
        "InheritedProtectedMemberDataSourceTests.cs"),
        _ => Task.CompletedTask);
}
