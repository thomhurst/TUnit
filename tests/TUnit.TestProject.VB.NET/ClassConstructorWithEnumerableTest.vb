Imports System
Imports Microsoft.Extensions.DependencyInjection
Imports TUnit
Imports TUnit.Core

<ClassConstructor(GetType(DependencyInjectionClassConstructor))>
<NotInParallel>
Public NotInheritable Class ClassConstructorWithEnumerableTest
    Implements IDisposable

    Private _isDisposed As Boolean
    Private ReadOnly _dummy As DummyReferenceTypeClass

    Public Sub New(dummy As DummyReferenceTypeClass)
        Me._dummy = dummy
    End Sub

    <Before(HookType.Test)>
    Public Sub Setup()
        If _isDisposed Then
            Throw New ObjectDisposedException(NameOf(ClassConstructorWithEnumerableTest))
        End If
    End Sub

    <Test>
    <MethodDataSource(NameOf(GetValues))>
    Public Sub DoSomething(value As Integer)
        ' Just use the dummy object that was injected
        If _dummy Is Nothing Then
            Throw New InvalidOperationException("Dummy object was not injected")
        End If
    End Sub

    Public Shared Function GetValues() As IEnumerable(Of Integer)
        Return New Integer() {1, 2, 3, 4}
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        _isDisposed = True
    End Sub
End Class
