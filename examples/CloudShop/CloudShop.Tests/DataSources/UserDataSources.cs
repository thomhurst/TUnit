using CloudShop.Shared.Contracts;

namespace CloudShop.Tests.DataSources;

/// <summary>
/// Provides test data for user/auth-related tests using [MethodDataSource].
/// Returns Func&lt;T&gt; for reference types to ensure proper test isolation (TUnit best practice).
/// </summary>
public static class UserDataSources
{
    private static int _counter;

    /// <summary>
    /// Valid registration requests with unique emails.
    /// Usage: [MethodDataSource(typeof(UserDataSources), nameof(NewUsers))]
    /// </summary>
    public static IEnumerable<Func<RegisterRequest>> NewUsers()
    {
        yield return () =>
        {
            var id = Interlocked.Increment(ref _counter);
            return new($"testuser-{id}@cloudshop.test", "TestPass123!", $"Test User {id}");
        };
        yield return () =>
        {
            var id = Interlocked.Increment(ref _counter);
            return new($"testuser-{id}@cloudshop.test", "TestPass456!", $"Test User {id}");
        };
        yield return () =>
        {
            var id = Interlocked.Increment(ref _counter);
            return new($"testuser-{id}@cloudshop.test", "TestPass789!", $"Test User {id}");
        };
    }

    /// <summary>
    /// Invalid login credentials that should fail authentication.
    /// </summary>
    public static IEnumerable<Func<LoginRequest>> InvalidCredentials()
    {
        yield return () => new("nonexistent@cloudshop.test", "password");
        yield return () => new("admin@cloudshop.test", "wrong-password");
        yield return () => new("", "");
        yield return () => new("invalid-email", "password");
    }
}
