namespace TUnit.TestProject.FSharp

open System.Collections.Concurrent
open System.Linq
open TUnit.Assertions
open TUnit.Assertions.Extensions
open TUnit.Core

// F# equivalent of SharedInjectedKeyedContainer
module SharedInjectedKeyedContainer =
    let instancesPerKey = ConcurrentDictionary<string, ResizeArray<DummyReferenceTypeClass>>()

type DummyReferenceTypeClass() = class end

[<ClassDataSource(typeof<DummyReferenceTypeClass>, Shared=SharedType.PerClass)>]
[<NotInParallel>]
type InjectSharedPerKey1(dummyReferenceTypeClass: DummyReferenceTypeClass) =
    [<Test>]
    [<Repeat(5)>]
    member _.Test1() = task {
        let testName = TestContext.Current.Value.TestDetails.TestName
        let found, list = SharedInjectedKeyedContainer.instancesPerKey.TryGetValue(testName)
        if found && list.Count > 0 then
            do! Assert.That(list).Contains(dummyReferenceTypeClass)
        for KeyValue(k, v) in SharedInjectedKeyedContainer.instancesPerKey do
            if k <> testName then
                do! Assert.That(list).DoesNotContain(dummyReferenceTypeClass)
        let l = SharedInjectedKeyedContainer.instancesPerKey.GetOrAdd(testName, fun _ -> ResizeArray())
        l.Add(dummyReferenceTypeClass)
        do! Assert.That(l.Distinct()).HasSingleItem()
    }
    [<Test>]
    [<Repeat(5)>]
    member _.Test2() = task {
        let testName = TestContext.Current.Value.TestDetails.TestName
        let found, list = SharedInjectedKeyedContainer.instancesPerKey.TryGetValue(testName)
        if found && list.Count > 0 then
            do! Assert.That(list).Contains(dummyReferenceTypeClass)
        for KeyValue(k, v) in SharedInjectedKeyedContainer.instancesPerKey do
            if k <> testName then
                do! Assert.That(list).DoesNotContain(dummyReferenceTypeClass)
        let l = SharedInjectedKeyedContainer.instancesPerKey.GetOrAdd(testName, fun _ -> ResizeArray())
        l.Add(dummyReferenceTypeClass)
        do! Assert.That(l.Distinct()).HasSingleItem()
    }
    [<Test>]
    [<Repeat(5)>]
    member _.Test3() = task {
        let testName = TestContext.Current.Value.TestDetails.TestName
        let found, list = SharedInjectedKeyedContainer.instancesPerKey.TryGetValue(testName)
        if found && list.Count > 0 then
            do! Assert.That(list).Contains(dummyReferenceTypeClass)
        for KeyValue(k, v) in SharedInjectedKeyedContainer.instancesPerKey do
            if k <> testName then
                do! Assert.That(list).DoesNotContain(dummyReferenceTypeClass)
        let l = SharedInjectedKeyedContainer.instancesPerKey.GetOrAdd(testName, fun _ -> ResizeArray())
        l.Add(dummyReferenceTypeClass)
        do! Assert.That(l.Distinct()).HasSingleItem()
    }

[<ClassDataSource(typeof<DummyReferenceTypeClass>, Shared=SharedType.PerClass)>]
[<NotInParallel>]
type InjectSharedPerKey2(dummyReferenceTypeClass: DummyReferenceTypeClass) =
    [<Test>]
    [<Repeat(5)>]
    member _.Test1() = task {
        let testName = TestContext.Current.Value.TestDetails.TestName
        let found, list = SharedInjectedKeyedContainer.instancesPerKey.TryGetValue(testName)
        if found && list.Count > 0 then
            do! Assert.That(list).Contains(dummyReferenceTypeClass)
        for KeyValue(k, v) in SharedInjectedKeyedContainer.instancesPerKey do
            if k <> testName then
                do! Assert.That(list).DoesNotContain(dummyReferenceTypeClass)
        let l = SharedInjectedKeyedContainer.instancesPerKey.GetOrAdd(testName, fun _ -> ResizeArray())
        l.Add(dummyReferenceTypeClass)
        do! Assert.That(l.Distinct()).HasSingleItem()
    }
    [<Test>]
    [<Repeat(5)>]
    member _.Test2() = task {
        let testName = TestContext.Current.Value.TestDetails.TestName
        let found, list = SharedInjectedKeyedContainer.instancesPerKey.TryGetValue(testName)
        if found && list.Count > 0 then
            do! Assert.That(list).Contains(dummyReferenceTypeClass)
        for KeyValue(k, v) in SharedInjectedKeyedContainer.instancesPerKey do
            if k <> testName then
                do! Assert.That(list).DoesNotContain(dummyReferenceTypeClass)
        let l = SharedInjectedKeyedContainer.instancesPerKey.GetOrAdd(testName, fun _ -> ResizeArray())
        l.Add(dummyReferenceTypeClass)
        do! Assert.That(l.Distinct()).HasSingleItem()
    }
    [<Test>]
    [<Repeat(5)>]
    member _.Test3() = task {
        let testName = TestContext.Current.Value.TestDetails.TestName
        let found, list = SharedInjectedKeyedContainer.instancesPerKey.TryGetValue(testName)
        if found && list.Count > 0 then
            do! Assert.That(list).Contains(dummyReferenceTypeClass)
        for KeyValue(k, v) in SharedInjectedKeyedContainer.instancesPerKey do
            if k <> testName then
                do! Assert.That(list).DoesNotContain(dummyReferenceTypeClass)
        let l = SharedInjectedKeyedContainer.instancesPerKey.GetOrAdd(testName, fun _ -> ResizeArray())
        l.Add(dummyReferenceTypeClass)
        do! Assert.That(l.Distinct()).HasSingleItem()
    }

[<ClassDataSource(typeof<DummyReferenceTypeClass>, Shared=SharedType.PerClass)>]
[<NotInParallel>]
type InjectSharedPerKey3(dummyReferenceTypeClass: DummyReferenceTypeClass) =
    [<Test>]
    [<Repeat(5)>]
    member _.Test1() = task {
        let testName = TestContext.Current.Value.TestDetails.TestName
        let found, list = SharedInjectedKeyedContainer.instancesPerKey.TryGetValue(testName)
        if found && list.Count > 0 then
            do! Assert.That(list).Contains(dummyReferenceTypeClass)
        for KeyValue(k, v) in SharedInjectedKeyedContainer.instancesPerKey do
            if k <> testName then
                do! Assert.That(list).DoesNotContain(dummyReferenceTypeClass)
        let l = SharedInjectedKeyedContainer.instancesPerKey.GetOrAdd(testName, fun _ -> ResizeArray())
        l.Add(dummyReferenceTypeClass)
        do! Assert.That(l.Distinct()).HasSingleItem()
    }
    [<Test>]
    [<Repeat(5)>]
    member _.Test2() = task {
        let testName = TestContext.Current.Value.TestDetails.TestName
        let found, list = SharedInjectedKeyedContainer.instancesPerKey.TryGetValue(testName)
        if found && list.Count > 0 then
            do! Assert.That(list).Contains(dummyReferenceTypeClass)
        for KeyValue(k, v) in SharedInjectedKeyedContainer.instancesPerKey do
            if k <> testName then
                do! Assert.That(list).DoesNotContain(dummyReferenceTypeClass)
        let l = SharedInjectedKeyedContainer.instancesPerKey.GetOrAdd(testName, fun _ -> ResizeArray())
        l.Add(dummyReferenceTypeClass)
        do! Assert.That(l.Distinct()).HasSingleItem()
    }
    [<Test>]
    [<Repeat(5)>]
    member _.Test3() = task {
        let testName = TestContext.Current.Value.TestDetails.TestName
        let found, list = SharedInjectedKeyedContainer.instancesPerKey.TryGetValue(testName)
        if found && list.Count > 0 then
            do! Assert.That(list).Contains(dummyReferenceTypeClass)
        for KeyValue(k, v) in SharedInjectedKeyedContainer.instancesPerKey do
            if k <> testName then
                do! Assert.That(list).DoesNotContain(dummyReferenceTypeClass)
        let l = SharedInjectedKeyedContainer.instancesPerKey.GetOrAdd(testName, fun _ -> ResizeArray())
        l.Add(dummyReferenceTypeClass)
        do! Assert.That(l.Distinct()).HasSingleItem()
    }