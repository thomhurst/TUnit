Imports System
Imports System.Threading.Tasks
Imports Microsoft.Extensions.DependencyInjection
Imports TUnit.Core
Imports TUnit.Core.Interfaces

Public Class DependencyInjectionClassConstructor
    Implements IClassConstructor

    Private ReadOnly _serviceProvider As IServiceProvider = CreateServiceProvider()

    Public Function Create(type As Type, classConstructorMetadata As ClassConstructorMetadata) As Task(Of Object) Implements IClassConstructor.Create
        Dim instance = ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, type)
        Return Task.FromResult(instance)
    End Function

    Private Shared Function CreateServiceProvider() As IServiceProvider
        Dim services = New ServiceCollection()
        services.AddTransient(Of DummyReferenceTypeClass)()
        Return services.BuildServiceProvider()
    End Function

End Class
