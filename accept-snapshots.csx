using System.IO;

var testDir = @"C:\git\TUnit\TUnit.Assertions.SourceGenerator.Tests";
var receivedFiles = Directory.GetFiles(testDir, "*.received.txt");

Console.WriteLine($"Found {receivedFiles.Length} received files");

foreach (var file in receivedFiles)
{
    var verifiedFile = file.Replace(".received.txt", ".verified.txt");
    File.Move(file, verifiedFile, overwrite: true);
    Console.WriteLine($"Moved: {Path.GetFileName(file)} -> {Path.GetFileName(verifiedFile)}");
}

Console.WriteLine("Done!");
