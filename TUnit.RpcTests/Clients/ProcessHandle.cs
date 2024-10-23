using CliWrap;

namespace TUnit.RpcTests.Clients;

public class ProcessHandle : IProcessHandle
{
    private readonly CommandTask<CommandResult> _commandTask;
    private readonly Stream _output;

    public ProcessHandle(CommandTask<CommandResult> commandTask, Stream output)
    {
        _commandTask = commandTask;
        _output = output;
        Id = commandTask.ProcessId;
        ProcessName = "dotnet";
    }
    
    public int Id { get; }
    public string ProcessName { get; }
    public int ExitCode { get; private set; }
    public TextWriter StandardInput  => new StringWriter();
    public TextReader StandardOutput => new StreamReader(_output);
    public void Dispose()
    {
        _commandTask.Dispose();
    }

    public void Kill()
    {
       Dispose(); 
    }

    public Task<int> StopAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<int> WaitForExitAsync()
    {
        var commandResult = await _commandTask;
        return ExitCode = commandResult.ExitCode;
    }

    public Task WriteInputAsync(string input)
    {
        return Task.CompletedTask;
    }
}