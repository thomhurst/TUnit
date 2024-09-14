﻿using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Dummy;

namespace TUnit.TestProject;

public class DependencyInjectionClassConstructor : IClassConstructor
{
    private readonly IServiceProvider _serviceProvider = CreateServiceProvider();
    
    private static readonly ConditionalWeakTable<object, IServiceScope> Scopes = new();

    public T Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>() where T : class
    {
        var scope = _serviceProvider.CreateAsyncScope();
        
        var instance = ActivatorUtilities.GetServiceOrCreateInstance<T>(scope.ServiceProvider);
        
        Scopes.Add(instance, scope);
        
        return instance;
    }

    public Task DisposeAsync<T>(T t)
    {
        if (t != null && Scopes.TryGetValue(t, out var scope))
        {
            scope.Dispose();
        }
        
        return Task.CompletedTask;
    }

    private static IServiceProvider CreateServiceProvider()
    {
        return new ServiceCollection()
            .AddTransient<SomeAsyncDisposableClass>()
            .BuildServiceProvider();
    }
}