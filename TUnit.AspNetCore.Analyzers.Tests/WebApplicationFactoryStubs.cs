namespace TUnit.AspNetCore.Analyzers.Tests;

internal static class WebApplicationFactoryStubs
{
    public const string Source = """
        namespace Microsoft.AspNetCore.Mvc.Testing
        {
            public class WebApplicationFactory<TEntryPoint> where TEntryPoint : class
            {
            }
        }

        namespace TUnit.AspNetCore
        {
            public class TestWebApplicationFactory<TEntryPoint> : Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<TEntryPoint>
                where TEntryPoint : class
            {
            }
        }

        public class Program { }
        """;
}
