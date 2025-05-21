namespace TUnit.TestProject.FSharp

open TUnit.Core

[<ClassConstructor(typeof<DependencyInjectionClassConstructor>)>]
type ClassConstructorTest(dummyReferenceTypeClass: DummyReferenceTypeClass) =

    member _.DummyReferenceTypeClass = dummyReferenceTypeClass

    [<Test>]
    member _.Test() = ()

