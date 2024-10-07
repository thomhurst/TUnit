using TUnit.Assertions.Extensions;
using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class ClassDataSourceDrivenTests2 : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ClassDataSourceDrivenTests2.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).HasCount().EqualTo(4);

            await AssertFileContains(generatedFiles[0], 
                    "global::TUnit.TestProject.ClassDataSourceDrivenTests2.Derived1 classArg = new global::TUnit.TestProject.ClassDataSourceDrivenTests2.Derived1();");

            await AssertFileContains(generatedFiles[0], 
                    "var resettableClassFactoryDelegate = () => new ResettableLazy<global::TUnit.TestProject.ClassDataSourceDrivenTests2>(() => new global::TUnit.TestProject.ClassDataSourceDrivenTests2(classArg));");


            await AssertFileContains(generatedFiles[1], 
                    "global::TUnit.TestProject.ClassDataSourceDrivenTests2.Derived2 classArg = new global::TUnit.TestProject.ClassDataSourceDrivenTests2.Derived2();");

            await AssertFileContains(generatedFiles[1], 
                    "var resettableClassFactoryDelegate = () => new ResettableLazy<global::TUnit.TestProject.ClassDataSourceDrivenTests2>(() => new global::TUnit.TestProject.ClassDataSourceDrivenTests2(classArg));");


            await AssertFileContains(generatedFiles[2], 
                    "global::TUnit.TestProject.ClassDataSourceDrivenTests2.Derived1 classArg = new global::TUnit.TestProject.ClassDataSourceDrivenTests2.Derived1();");

            await AssertFileContains(generatedFiles[2], 
                    "var resettableClassFactoryDelegate = () => new ResettableLazy<global::TUnit.TestProject.ClassDataSourceDrivenTests2>(() => new global::TUnit.TestProject.ClassDataSourceDrivenTests2(classArg));");


            await AssertFileContains(generatedFiles[3], 
                    "global::TUnit.TestProject.ClassDataSourceDrivenTests2.Derived2 classArg = new global::TUnit.TestProject.ClassDataSourceDrivenTests2.Derived2();");

            await AssertFileContains(generatedFiles[3], 
                    "var resettableClassFactoryDelegate = () => new ResettableLazy<global::TUnit.TestProject.ClassDataSourceDrivenTests2>(() => new global::TUnit.TestProject.ClassDataSourceDrivenTests2(classArg));");
        });
}