﻿namespace TUnit.TestProject.BeforeTests;

public class TestDiscoveryBeforeHooks
{
    // TODO: The "Before(TestDiscovery)" hook is currently not being called/source generated
    [Before(TestDiscovery)]
    public static async Task BeforeTestDiscovery(BeforeTestDiscoveryContext context)
    {
        await Task.CompletedTask;
    }

    [BeforeEvery(TestDiscovery)]
    public static async Task BeforeEveryTestDiscovery(BeforeTestDiscoveryContext context)
    {
        await File.WriteAllTextAsync($"TestDiscoveryBeforeTests{Guid.NewGuid():N}.txt", $"Blah!");
    }
}

public class TestDiscoveryBeforeTests
{
    [Test]
    public void EnsureBeforeEveryTestDiscovoryHit()
    {
        // Dummy
    }
}