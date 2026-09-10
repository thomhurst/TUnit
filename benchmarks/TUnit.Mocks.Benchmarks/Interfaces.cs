namespace TUnit.Mocks.Benchmarks;

/// <summary>
/// Common interfaces used across all mock benchmarks.
/// Each mocking library will mock these same interfaces.
/// </summary>
public interface ICalculatorService
{
    int Add(int a, int b);
    double Divide(double numerator, double denominator);
    string Format(int value);
}

public interface IUserRepository
{
    User? GetById(int id);
    IReadOnlyList<User> GetAll();
    void Save(User user);
    void Delete(int id);
    bool Exists(int id);
}

public interface INotificationService
{
    void Send(string recipient, string message);
    Task SendAsync(string recipient, string message);
    bool IsAvailable();
}

public interface ILogger
{
    void Log(string level, string message);
    void LogError(string message, Exception exception);
    bool IsEnabled(string level);
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
