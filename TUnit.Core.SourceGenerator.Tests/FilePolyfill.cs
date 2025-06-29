namespace TUnit.Core.SourceGenerator.Tests;

public static class FilePolyfill
{
    public static async Task<string> ReadAllTextAsync(string path)
    {
#if NETFRAMEWORK
        return await Task.Run(() => File.ReadAllText(path));
#else
        return await File.ReadAllTextAsync(path);
#endif
    }

    public static async Task WriteAllTextAsync(string path, string contents)
    {
#if NETFRAMEWORK
        await Task.Run(() => File.WriteAllText(path, contents));
#else
        await File.WriteAllTextAsync(path, contents);
#endif
    }
}
