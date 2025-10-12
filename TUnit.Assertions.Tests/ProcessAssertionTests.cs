using TUnit.Assertions.Extensions;
using System.Diagnostics;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class ProcessAssertionTests
{
    [Test]
    public async Task Test_Process_HasNotExited()
    {
        var currentProcess = Process.GetCurrentProcess();
        try
        {
            await Assert.That(currentProcess).HasNotExited();
        }
        finally
        {
            currentProcess.Dispose();
        }
    }

    [Test]
    public async Task Test_Process_HasNotExited_CurrentProcess()
    {
        using var currentProcess = Process.GetCurrentProcess();
        await Assert.That(currentProcess).HasNotExited();
    }

    [Test]
    public async Task Test_Process_HasExited()
    {
        // Start a process that exits immediately
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "--version",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process != null)
        {
            process.WaitForExit();
            await Assert.That(process).HasExited();
        }
    }

    [Test]
    public async Task Test_Process_Responding()
    {
        var currentProcess = Process.GetCurrentProcess();
        try
        {
            await Assert.That(currentProcess).Responding();
        }
        finally
        {
            currentProcess.Dispose();
        }
    }

    [Test]
    public async Task Test_Process_DoesNotHaveEventRaisingEnabled()
    {
        // By default, EnableRaisingEvents is false
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "--version",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        await Assert.That(process).DoesNotHaveEventRaisingEnabled();
    }

    [Test]
    public async Task Test_Process_HasEventRaisingEnabled()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "--version",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        await Assert.That(process).EnableRaisingEvents();
    }
}
