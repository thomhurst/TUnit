using TUnit.Core;

namespace TUnit.Playwright;

internal static class PlaywrightArtifactNames
{
    // Display names repeat the method arguments and can be very long. Keep paths well
    // under the Windows MAX_PATH limit; the test id hash keeps truncated names unique.
    private const int MaxDisplayNameLength = 80;

    /// <summary>
    /// Returns <c>&lt;test id hash&gt;_&lt;display name&gt;</c>, plus <c>-attemptN</c> for retries.
    /// The full test id is unique but long, so a stable hash of it keeps the name short.
    /// </summary>
    internal static string ForTest(TestContext context, int attempt)
    {
        var displayName = Sanitize(context.Metadata.DisplayName);
        if (displayName.Length > MaxDisplayNameLength)
        {
            displayName = displayName.Substring(0, MaxDisplayNameLength);
        }

        return $"{StableHash(context.Metadata.TestDetails.TestId):x8}_{displayName}" +
               (attempt > 0 ? $"-attempt{attempt + 1}" : string.Empty);
    }

    /// <summary>
    /// Creates an empty file at a free path and returns it. Creating the file claims the name,
    /// so concurrent writers never pick the same path.
    /// </summary>
    internal static string Reserve(string directory, string name, string extension)
    {
        Directory.CreateDirectory(directory);
        var target = Path.Combine(directory, name + extension);
        for (var n = 2; ; n++)
        {
            try
            {
                using (new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete))
                {
                }

                return target;
            }
            catch (IOException) when (File.Exists(target) && n < 1000)
            {
                target = Path.Combine(directory, $"{name}-{n}{extension}");
            }
        }
    }

    internal static string Sanitize(string value)
    {
        var characters = value.ToCharArray();
        var invalidCharacters = Path.GetInvalidFileNameChars();
        for (var i = 0; i < characters.Length; i++)
        {
            if (Array.IndexOf(invalidCharacters, characters[i]) >= 0)
            {
                characters[i] = '_';
            }
            else if (characters[i] == ' ')
            {
                characters[i] = '-';
            }
        }

        var name = characters.Length == 0 ? "test" : new string(characters);
        // Windows device names remain reserved even when followed by an extension.
        var dot = name.IndexOf('.');
        var stem = (dot < 0 ? name : name.Substring(0, dot)).ToUpperInvariant();
        var reserved = stem is "CON" or "PRN" or "AUX" or "NUL" or "CONIN$" or "CONOUT$" ||
                       (stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal) || stem.StartsWith("LPT", StringComparison.Ordinal)) &&
                        stem[3] is >= '1' and <= '9' or '¹' or '²' or '³');
        return reserved ? "_" + name : name;
    }

    // FNV-1a: string.GetHashCode is randomized per process, and names must be stable across runs.
    private static uint StableHash(string value)
    {
        var hash = 2166136261;
        foreach (var character in value)
        {
            hash = (hash ^ character) * 16777619;
        }

        return hash;
    }
}
