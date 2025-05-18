﻿using System.Diagnostics.CodeAnalysis;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._1914;

[SkipNetFramework("ExecutionContext.Restore is not supported on .NET Framework")]
[UnconditionalSuppressMessage("Usage", "TUnit0042:Global hooks should not be mixed with test classes to avoid confusion. Place them in their own class.")]
[UnconditionalSuppressMessage("Usage", "TUnit0047:Call `context.AddAsyncLocalValues`")]
public class AsyncHookTests
{
    private static readonly AsyncLocal<string> _0BeforeTestDiscoveryLocal = new();
    private static readonly AsyncLocal<string> _0BeforeTestDiscoveryLocal2 = new();
    
    private static readonly AsyncLocal<string> _1BeforeTestSessionLocal = new();
    private static readonly AsyncLocal<string> _1BeforeTestSessionLocal2 = new();
    
    private static readonly AsyncLocal<string> _2BeforeAssemblyLocal = new();
    private static readonly AsyncLocal<string> _2BeforeAssemblyLocal2 = new();
    
    private static readonly AsyncLocal<string> _3BeforeClassLocal = new();
    private static readonly AsyncLocal<string> _3BeforeClassLocal2 = new();
    
    private static readonly AsyncLocal<string> _4BeforeTestLocal = new();
    private static readonly AsyncLocal<string> _4BeforeTestLocal2 = new();

    [Before(TestDiscovery)]
    public static async Task BeforeTestDiscovery(BeforeTestDiscoveryContext context)
    {
        await Task.CompletedTask;
        _0BeforeTestDiscoveryLocal.Value = "BeforeTestDiscovery";
#if NET
        context.AddAsyncLocalValues();
#endif
    }
    
    [Before(TestDiscovery)]
    public static async Task BeforeTestDiscovery2(BeforeTestDiscoveryContext context)
    {
        await Task.CompletedTask;
        _0BeforeTestDiscoveryLocal2.Value = "BeforeTestDiscovery2";
#if NET
        context.AddAsyncLocalValues();
#endif
    }

    [Before(TestSession)]
    public static async Task BeforeTestSession(TestSessionContext context)
    {
        await Task.CompletedTask;
        _1BeforeTestSessionLocal.Value = "BeforeTestSession";
#if NET
        context.AddAsyncLocalValues();
#endif
    }
    
    [Before(TestSession)]
    public static async Task BeforeTestSession2(TestSessionContext context)
    {
        await Task.CompletedTask;
        _1BeforeTestSessionLocal2.Value = "BeforeTestSession2";
#if NET
        context.AddAsyncLocalValues();
#endif
    }

    [Before(Assembly)]
    public static async Task BeforeAssembly(AssemblyHookContext context)
    {
        await Task.CompletedTask;
        _2BeforeAssemblyLocal.Value = "BeforeAssembly";
#if NET
        context.AddAsyncLocalValues();
#endif
    }
    
    [Before(Assembly)]
    public static async Task BeforeAssembly2(AssemblyHookContext context)
    {
        await Task.CompletedTask;
        _2BeforeAssemblyLocal2.Value = "BeforeAssembly2";
#if NET
        context.AddAsyncLocalValues();
#endif
    }

    [Before(Class)]
    public static async Task BeforeClass(ClassHookContext context)
    {
        await Task.CompletedTask;
        _3BeforeClassLocal.Value = "BeforeClass";
#if NET
        context.AddAsyncLocalValues();
#endif
    }
    
    [Before(Class)]
    public static async Task BeforeClass2(ClassHookContext context)
    {
        await Task.CompletedTask;
        _3BeforeClassLocal2.Value = "BeforeClass2";
#if NET
        context.AddAsyncLocalValues();
#endif
    }

    [Before(Test)]
    public async Task BeforeTest(TestContext context)
    {
        await Task.CompletedTask;
        _4BeforeTestLocal.Value = "BeforeTest";
#if NET
        context.AddAsyncLocalValues();
#endif
    }
    
    [Before(Test)]
    public async Task BeforeTest2(TestContext context)
    {
        await Task.CompletedTask;
        _4BeforeTestLocal2.Value = "BeforeTest2";
#if NET
        context.AddAsyncLocalValues();
#endif
    }


    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    [Arguments(5)]
    [Arguments(6)]
    [Arguments(7)]
    [Arguments(8)]
    public async Task TestAsyncLocal(int i)
    {
        await Assert.That(_0BeforeTestDiscoveryLocal.Value).IsEqualTo("BeforeTestDiscovery");
        await Assert.That(_1BeforeTestSessionLocal.Value).IsEqualTo("BeforeTestSession");
        await Assert.That(_2BeforeAssemblyLocal.Value).IsEqualTo("BeforeAssembly");
        await Assert.That(_3BeforeClassLocal.Value).IsEqualTo("BeforeClass");
        await Assert.That(_4BeforeTestLocal.Value).IsEqualTo("BeforeTest");
        
        await Assert.That(_0BeforeTestDiscoveryLocal2.Value).IsEqualTo("BeforeTestDiscovery2");
        await Assert.That(_1BeforeTestSessionLocal2.Value).IsEqualTo("BeforeTestSession2");
        await Assert.That(_2BeforeAssemblyLocal2.Value).IsEqualTo("BeforeAssembly2");
        await Assert.That(_3BeforeClassLocal2.Value).IsEqualTo("BeforeClass2");
        await Assert.That(_4BeforeTestLocal2.Value).IsEqualTo("BeforeTest2");
    }
}