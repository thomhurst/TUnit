Imports System
Imports TUnit.Core

' VB.NET does not support generic attributes, so you cannot use <ClassDataSource(Of DataClass)> or <ClassConstructor(Of DependencyInjectionClassConstructor)>.
' If your test framework provides non-generic alternatives that accept a Type parameter, use them as shown below.
' Otherwise, these attributes must be omitted or handled in C#.

' <ClassDataSource(GetType(DataClass))>
' <ClassConstructor(GetType(DependencyInjectionClassConstructor))>
'Public Class AndEvenMoreTests

'    Private ReadOnly _dataClass As DataClass

'    Public Sub New(dataClass As DataClass)
'        _dataClass = dataClass
'    End Sub

'    <Test>
'    Public Sub HaveFun()
'        Console.WriteLine(_dataClass)
'        Console.WriteLine("For more information, check out the documentation")
'        Console.WriteLine("https://tunit.dev/")
'    End Sub
'End Class