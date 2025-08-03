Imports System
Imports TUnit.Core

<Arguments("Hello")>
<Arguments("World")>
Public Class MoreTests
    Private ReadOnly _title As String

    Public Sub New(title As String)
        _title = title
    End Sub

    <Test>
    Public Sub ClassLevelDataRow()
        Console.WriteLine(_title)
        Console.WriteLine("Did I forget that data injection works on classes too?")
    End Sub

    <ClassDataSource(GetType(DataClass), Shared:=SharedType.PerTestSession)>
    Public Property DataClass As DataClass

    <Test>
    <MatrixDataSource>
    Public Sub Matrices(
        <Matrix(1, 2, 3)> a As Integer,
        <Matrix(True, False)> b As Boolean,
        <Matrix("A", "B", "C")> c As String)
        Console.WriteLine("A new test will be created for each data row, whether it's on the class or method level!")
        Console.WriteLine("Oh and this is a matrix test. That means all combinations of inputs are attempted.")
    End Sub

End Class
