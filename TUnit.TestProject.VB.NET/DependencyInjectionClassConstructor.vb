Imports System
Imports System.Threading.Tasks
Imports Microsoft.Extensions.DependencyInjection
Imports TUnit.Core
Imports TUnit.Core.Interfaces

Public Class DependencyInjectionClassConstructor
    Implements IClassConstructor
    Implements ITestEndEventReceiver

    Private ReadOnly _serviceProvider As IServiceProvider = CreateServiceProvider()
    Private _scope As AsyncServiceScope? = Nothing

    Public Function Create(type As Type, classConstructorMetadata As ClassConstructorMetadata) As Object Implements IClassConstructor.Create
        If _scope Is Nothing Then
            _scope = _serviceProvider.CreateAsyncScope()
        End If
        Return ActivatorUtilities.GetServiceOrCreateInstance(_scope.Value.ServiceProvider, type)
    End Function

    Public Function OnTestEnd(testContext As AfterTestContext) As ValueTask Implements ITestEndEventReceiver.OnTestEnd
        Return _scope.Value.DisposeAsync()
    End Function

    Private Shared Function CreateServiceProvider() As IServiceProvider
        Return New ServiceCollection().
            AddTransient(Of DummyReferenceTypeClass)().
            BuildServiceProvider()
    End Function

    Public ReadOnly Property Order As Integer Implements IEventReceiver.Order
        Get
            Return 0
        End Get
    End Property
End Class