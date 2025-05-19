Imports TUnit.Core

<ClassConstructor(GetType(DependencyInjectionClassConstructor))>
Public Class ClassConstructorTest

    Public Sub New(dummyReferenceTypeClass As DummyReferenceTypeClass)
        Me.DummyReferenceTypeClass = dummyReferenceTypeClass
    End Sub

    Public ReadOnly Property DummyReferenceTypeClass As DummyReferenceTypeClass

    <Test>
    Public Sub Test()
        ' Test logic here
    End Sub

End Class