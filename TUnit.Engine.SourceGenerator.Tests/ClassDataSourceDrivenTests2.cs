﻿using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class ClassDataSourceDrivenTests2 : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ClassDataSourceDrivenTests2.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles, Has.Length.EqualTo(4));

            Assert.That(generatedFiles[0],
                Does.Contain(
                    "global::TUnit.TestProject.ClassDataSourceDrivenTests2.Derived1 classArg = new global::TUnit.TestProject.ClassDataSourceDrivenTests2.Derived1();"));

            Assert.That(generatedFiles[0],
                Does.Contain(
                    "var resettableClassFactoryDelegate = () => new ResettableLazy<global::TUnit.TestProject.ClassDataSourceDrivenTests2>(() => new global::TUnit.TestProject.ClassDataSourceDrivenTests2(classArg));"));


            Assert.That(generatedFiles[1],
                Does.Contain(
                    "global::TUnit.TestProject.ClassDataSourceDrivenTests2.Derived2 classArg = new global::TUnit.TestProject.ClassDataSourceDrivenTests2.Derived2();"));

            Assert.That(generatedFiles[1],
                Does.Contain(
                    "var resettableClassFactoryDelegate = () => new ResettableLazy<global::TUnit.TestProject.ClassDataSourceDrivenTests2>(() => new global::TUnit.TestProject.ClassDataSourceDrivenTests2(classArg));"));


            Assert.That(generatedFiles[2],
                Does.Contain(
                    "global::TUnit.TestProject.ClassDataSourceDrivenTests2.Derived1 classArg = new global::TUnit.TestProject.ClassDataSourceDrivenTests2.Derived1();"));

            Assert.That(generatedFiles[2],
                Does.Contain(
                    "var resettableClassFactoryDelegate = () => new ResettableLazy<global::TUnit.TestProject.ClassDataSourceDrivenTests2>(() => new global::TUnit.TestProject.ClassDataSourceDrivenTests2(classArg));"));


            Assert.That(generatedFiles[3],
                Does.Contain(
                    "global::TUnit.TestProject.ClassDataSourceDrivenTests2.Derived2 classArg = new global::TUnit.TestProject.ClassDataSourceDrivenTests2.Derived2();"));

            Assert.That(generatedFiles[3],
                Does.Contain(
                    "var resettableClassFactoryDelegate = () => new ResettableLazy<global::TUnit.TestProject.ClassDataSourceDrivenTests2>(() => new global::TUnit.TestProject.ClassDataSourceDrivenTests2(classArg));"));
        });
}