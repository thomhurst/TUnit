using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.Generators.Expansion;

public interface ITestExpander
{
    bool CanExpand(TestMethodMetadata testInfo);
    int GenerateExpansions(CodeWriter writer, TestMethodMetadata testInfo, int variantIndex);
}