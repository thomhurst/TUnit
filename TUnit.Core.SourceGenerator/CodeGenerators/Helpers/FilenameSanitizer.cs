namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

public static class FilenameSanitizer
{
    public static string Sanitize(string filename)
    {
        var sanitizedFilename = filename
            .Replace(':', '_')
            .Replace('.', '_')
            .Replace('-', '_');

        return $"{sanitizedFilename}_{Guid.NewGuid():N}";
    }
}