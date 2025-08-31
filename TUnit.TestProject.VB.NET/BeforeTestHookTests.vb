Imports TUnit.Core

Public Class BeforeTestHookTests
    Private Shared HookExecuted As Boolean = False

    <Before(HookType.Test)>
    Public Sub TestInitialize()
        Console.WriteLine("Before hook executed")
        HookExecuted = True
    End Sub

    <Test>
    Public Sub BasicTest()
        Console.WriteLine("This is a basic test")
        ' Verify that the hook was executed
        If Not HookExecuted Then
            Throw New Exception("Before hook was not executed!")
        End If
    End Sub

    <After(HookType.Test)>
    Public Sub TestCleanup()
        Console.WriteLine("After hook executed")
        HookExecuted = False
    End Sub
End Class