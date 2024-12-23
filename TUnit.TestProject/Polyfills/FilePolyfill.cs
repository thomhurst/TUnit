using System.Text;

namespace TUnit.TestProject.Polyfills;

public class FilePolyfill
{
    public static async Task WriteAllTextAsync(string path, string content)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);

        var encodedText = Encoding.UTF8.GetBytes(content);

        await fileStream.WriteAsync(encodedText, 0, encodedText.Length);
    }

    public static async Task AppendAllLinesAsync(string path, IEnumerable<string> lines)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var fileStream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.None, 4096, useAsync: true);

        foreach (var line in lines)
        {
            var encodedText = Encoding.UTF8.GetBytes(line + Environment.NewLine);
            await fileStream.WriteAsync(encodedText, 0, encodedText.Length);
        }
    }
}