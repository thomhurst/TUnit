using System.Text;

namespace TUnit.Engine.SourceGenerator;

public static class SourceCodeFormatter
{
    public static string Format(string input)
    {
        var lines = input.Split(['\n'], StringSplitOptions.None)
            .Select(x => x.Trim());

        var indentationLevel = 0;

        var stringBuilder = new StringBuilder();
        
        foreach (var line in lines)
        {
            if (line.StartsWith("}"))
            {
                indentationLevel--;
            }
            
            stringBuilder.AppendLine($"{new string('\t', indentationLevel)}{line}");
            
            if (line.StartsWith("{"))
            {
                indentationLevel++;
            }
        }

        return stringBuilder.ToString();
    }
}