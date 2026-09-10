Imports System
Imports System.Runtime.CompilerServices
Imports TUnit.Core

' This simulates a Reqnroll-generated test class with CompilerGeneratedAttribute
<CompilerGenerated>
Public Class CompilerGeneratedTest
    <Test>
    Public Sub GeneratedTestMethod()
        ' This test should be executed even though the class is marked as CompilerGenerated
        Console.WriteLine("Generated test executed successfully")
    End Sub
    
    <Test>
    <Arguments(1, 2, 3)>
    <Arguments(2, 3, 5)>
    Public Sub GeneratedTestWithArguments(a As Integer, b As Integer, expected As Integer)
        Dim result = a + b
        If result <> expected Then
            Throw New Exception($"Expected {expected}, but got {result}")
        End If
        Console.WriteLine($"Generated test with arguments: {a} + {b} = {result}")
    End Sub
End Class