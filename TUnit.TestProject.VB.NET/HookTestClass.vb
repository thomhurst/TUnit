Imports System
Imports TUnit.Core

Public Class HookTestClass

    <Before(HookType.Test)>
    Public Sub BeforeTestHook()
        Console.WriteLine("VB.NET Hook: Before Test Hook executed!")
    End Sub

    <After(HookType.Test)>
    Public Sub AfterTestHook()
        Console.WriteLine("VB.NET Hook: After Test Hook executed!")
    End Sub

    <Test>
    Public Sub TestWithHooks()
        Console.WriteLine("VB.NET Test: Test method executing")
    End Sub

End Class