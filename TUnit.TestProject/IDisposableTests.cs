﻿using TUnit.Core;

namespace TUnit.TestProject;

public class IDisposableTests : IDisposable
{
    [Test]
    public void One()
    {
        
    }
    
    [Test]
    public Task Two()
    {
        return Task.CompletedTask;
    }
    
    [Test]
    public async Task Three()
    {
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        
    }
}