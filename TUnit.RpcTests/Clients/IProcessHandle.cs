namespace TUnit.RpcTests.Clients;

public interface IProcessHandle
{
    int Id { get; }

    string ProcessName { get; }

    int ExitCode { get; }

    TextWriter StandardInput { get; }

    TextReader StandardOutput { get; }

    void Dispose();

    void Kill();

    Task<int> StopAsync();

    Task<int> WaitForExitAsync();

    Task WriteInputAsync(string input);
}
