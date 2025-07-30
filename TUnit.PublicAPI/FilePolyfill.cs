using System.IO;

namespace TUnit.PublicAPI;

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
        // Ensure we write with Unix line endings by using UTF8 encoding without BOM
        var utf8WithoutBom = new System.Text.UTF8Encoding(false);
        
#if NETFRAMEWORK
        await Task.Run(() => 
        {
            using (var writer = new StreamWriter(path, false, utf8WithoutBom))
            {
                writer.Write(contents);
            }
        });
#else
        using (var writer = new StreamWriter(path, false, utf8WithoutBom))
        {
            await writer.WriteAsync(contents);
        }
#endif
    }
}