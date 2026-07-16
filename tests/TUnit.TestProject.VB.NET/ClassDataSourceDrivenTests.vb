Imports System.Threading.Tasks
Imports TUnit.Assertions
Imports TUnit.Assertions.Extensions
Imports TUnit.Core
Imports TUnit.Core.Interfaces
Imports TUnit.TestProject.Library.Models


Public Class ClassDataSourceDrivenTests

    <Test>
    <ClassDataSource(GetType(SomeAsyncDisposableClass))>
    Public Sub DataSource_Class(value As SomeAsyncDisposableClass)
        ' Dummy method
    End Sub

    <Test>
    <ClassDataSource(GetType(SomeAsyncDisposableClass))>
    Public Sub DataSource_Class_Generic(value As SomeAsyncDisposableClass)
        ' Dummy method
    End Sub

    <Test>
    <ClassDataSource(GetType(InitializableClass))>
    Public Async Function IsInitialized_With_1_ClassDataSource(class1 As InitializableClass) As Task
        Await Assert.That(class1.IsInitialized).IsTrue()
    End Function

    <Test>
    <ClassDataSource(GetType(InitializableClass), GetType(InitializableClass))>
    Public Async Function IsInitialized_With_2_ClassDataSources(class1 As InitializableClass, class2 As InitializableClass) As Task
        Await Assert.That(class1.IsInitialized).IsTrue()
        Await Assert.That(class2.IsInitialized).IsTrue()
    End Function

    <Test>
    <ClassDataSource(GetType(InitializableClass), GetType(InitializableClass), GetType(InitializableClass))>
    Public Async Function IsInitialized_With_3_ClassDataSources(class1 As InitializableClass, class2 As InitializableClass, class3 As InitializableClass) As Task
        Await Assert.That(class1.IsInitialized).IsTrue()
        Await Assert.That(class2.IsInitialized).IsTrue()
        Await Assert.That(class3.IsInitialized).IsTrue()
    End Function

    <Test>
    <ClassDataSource(GetType(InitializableClass), GetType(InitializableClass), GetType(InitializableClass), GetType(InitializableClass))>
    Public Async Function IsInitialized_With_4_ClassDataSources(class1 As InitializableClass, class2 As InitializableClass, class3 As InitializableClass, class4 As InitializableClass) As Task
        Await Assert.That(class1.IsInitialized).IsTrue()
        Await Assert.That(class2.IsInitialized).IsTrue()
        Await Assert.That(class3.IsInitialized).IsTrue()
        Await Assert.That(class4.IsInitialized).IsTrue()
    End Function

    <Test>
    <ClassDataSource(GetType(InitializableClass), GetType(InitializableClass), GetType(InitializableClass), GetType(InitializableClass), GetType(InitializableClass))>
    Public Async Function IsInitialized_With_5_ClassDataSources(class1 As InitializableClass, class2 As InitializableClass, class3 As InitializableClass, class4 As InitializableClass, class5 As InitializableClass) As Task
        Await Assert.That(class1.IsInitialized).IsTrue()
        Await Assert.That(class2.IsInitialized).IsTrue()
        Await Assert.That(class3.IsInitialized).IsTrue()
        Await Assert.That(class4.IsInitialized).IsTrue()
        Await Assert.That(class5.IsInitialized).IsTrue()
    End Function

End Class

Public Class InitializableClass
    Implements IAsyncInitializer

    Public Property IsInitialized As Boolean

    Public Function InitializeAsync() As Task Implements IAsyncInitializer.InitializeAsync
        IsInitialized = True
        Return Task.CompletedTask
    End Function
End Class
