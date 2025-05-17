Imports System
Imports System.Threading.Tasks
Imports TUnit.Core.Interfaces

Public Class DataClass
    Implements IAsyncInitializer, IAsyncDisposable

    Public Function InitializeAsync() As Task Implements IAsyncInitializer.InitializeAsync
        Return Console.Out.WriteLineAsync("Classes can be injected into tests, and they can perform some initialisation logic such as starting an in-memory server or a test container.")
    End Function

    Public Function DisposeAsync() As ValueTask Implements IAsyncDisposable.DisposeAsync
        Return New ValueTask(DisposeAsyncTask())
    End Function

    Private Async Function DisposeAsyncTask() As Task
        Await Console.Out.WriteLineAsync("And when the class is finished with, we can clean up any resources.")
    End Function
End Class
