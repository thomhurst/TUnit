﻿using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Hooks;

[UnconditionalSuppressMessage("Trimming", "IL2111:Method with parameters or return value with `DynamicallyAccessedMembersAttribute` is accessed via reflection. Trimmer can\'t guarantee availability of the requirements of the method.")]
public class LastTestInClassAdapter(ILastTestInClassEventReceiver lastTestInClassEventReceiver, TestContext testContext) : IExecutableHook<ClassHookContext>
{
    public string Name => nameof(lastTestInClassEventReceiver.OnLastTestInClass);

    [field: AllowNull, MaybeNull]
    public MethodMetadata MethodInfo => field ??= new MethodMetadata
    {
        Type = typeof(ILastTestInClassEventReceiver),
        TypeReference = TypeReference.CreateConcrete(typeof(ILastTestInClassEventReceiver).AssemblyQualifiedName!),
        Name = nameof(lastTestInClassEventReceiver.OnLastTestInClass),
        Parameters = [new ParameterMetadata<ClassHookContext>
        {
            TypeReference = TypeReference.CreateConcrete(typeof(ClassHookContext).AssemblyQualifiedName!),
            Name = "context",
            ReflectionInfo = typeof(ILastTestInClassEventReceiver).GetMethod(nameof(ILastTestInClassEventReceiver.OnLastTestInClass))!.GetParameters()[0],
        }, new ParameterMetadata<TestContext>
        {
            TypeReference = TypeReference.CreateConcrete(typeof(TestContext).AssemblyQualifiedName!),
            Name = "testContext",
            ReflectionInfo = typeof(ILastTestInClassEventReceiver).GetMethod(nameof(ILastTestInClassEventReceiver.OnLastTestInClass))!.GetParameters()[0],
        }],
        GenericTypeCount = 0,
        ReturnType = typeof(ValueTask),
        ReturnTypeReference = TypeReference.CreateConcrete(typeof(ValueTask).AssemblyQualifiedName!),
        Class = new ClassMetadata
        {
            Parent = null,
            Type = typeof(ILastTestInClassEventReceiver),
            TypeReference = TypeReference.CreateConcrete(typeof(ILastTestInClassEventReceiver).AssemblyQualifiedName!),
            Assembly = new AssemblyMetadata
            {
                Name = "TUnit.Core",
            },
            Namespace = "TUnit.Core.Interfaces",
            Name = "ILastTestInClassEventReceiver",
            Parameters = [],
            Properties = [],
        }
    };

    public int Order => 0;

    public bool Execute(ClassHookContext context, CancellationToken cancellationToken)
    {
        return false;
    }

    public async ValueTask ExecuteAsync(ClassHookContext context, CancellationToken cancellationToken)
    {
        await lastTestInClassEventReceiver.OnLastTestInClass(context, testContext);
    }
}
