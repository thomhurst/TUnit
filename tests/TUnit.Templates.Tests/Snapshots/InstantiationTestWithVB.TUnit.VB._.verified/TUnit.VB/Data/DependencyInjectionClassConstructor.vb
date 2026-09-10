Imports System
Imports TUnit.Core
Imports TUnit.Core.Interfaces
Public Class DependencyInjectionClassConstructor
    Implements IClassConstructor

    Public Function Create(Of T As Class)(classConstructorMetadata As ClassConstructorMetadata) As T
        Console.WriteLine("You can also control how your test classes are new'd up, giving you lots of power and the ability to utilise tools such as dependency injection")

        If GetType(T) Is GetType(AndEvenMoreTests) Then
            Return CType(DirectCast(New AndEvenMoreTests(New DataClass()), Object), T)
        End If

        Throw New NotImplementedException()
    End Function

    ' Explicit interface implementation for IClassConstructor.Create(Type, ClassConstructorMetadata)
    Private Function IClassConstructor_Create(type As Type, classConstructorMetadata As ClassConstructorMetadata) As Task(Of Object) Implements IClassConstructor.Create
        If type Is GetType(AndEvenMoreTests) Then
            Return Task.FromResult(Of Object)(New AndEvenMoreTests(New DataClass()))
        End If

        Throw New NotImplementedException()
    End Function

End Class
