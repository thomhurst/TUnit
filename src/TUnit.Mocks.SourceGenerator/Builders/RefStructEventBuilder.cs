using System.Linq;
using TUnit.Mocks.SourceGenerator.Models;

namespace TUnit.Mocks.SourceGenerator.Builders;

/// <summary>
/// Connects the member surface to file-local implementations without boxing event arguments.
/// Each event uses its primary or secondary surface's identity so the same raise extension
/// works with single-type, multi-type, partial, and wrap mocks.
/// </summary>
internal static class RefStructEventBuilder
{
    private static bool NeedsInterface(MockEventModel evt)
        => evt.HasRefStructParams && evt.IsSignatureAccessibleFromAssembly && !evt.IsStaticAbstract;

    public static string GetInterfaceName(MockTypeModel model, MockEventModel evt)
    {
        var surfaceName = model.FullyQualifiedName;
        if (model.IsSecondaryMemberSurface)
        {
            surfaceName += "_" + model.AdditionalInterfaceNames[0];
        }
        else if (evt.OwnerTypeIndex > 0)
        {
            surfaceName += "_" + model.AdditionalInterfaceNames[evt.OwnerTypeIndex - 1];
        }

        return $"{MockImplBuilder.GetSafeName(surfaceName)}_{evt.Name}_Raiser{MockImplBuilder.GetTypeParameterList(model)}";
    }

    public static string GetInterfaceType(MockTypeModel model, MockEventModel evt)
        => $"global::{MockImplBuilder.MemberSurfaceNamespace}.{GetInterfaceName(model, evt)}";

    public static string GetBaseInterfaces(MockTypeModel model)
        => string.Concat(model.Events.Where(NeedsInterface).Select(evt => ", " + GetInterfaceType(model, evt)));

    public static string GetParameters(MockEventModel evt)
        => string.Join(", ", evt.RaiseParameterList.Select(p => $"{p.FullyQualifiedType} {p.Name}"));

    public static string GetArguments(MockEventModel evt)
        => string.Join(", ", evt.RaiseParameterList.Select(p => p.Name));

    public static void EmitInterfaces(CodeWriter writer, MockTypeModel model)
    {
        foreach (var evt in model.Events.Where(NeedsInterface))
        {
            using (writer.Block($"internal interface {GetInterfaceName(model, evt)}{MockImplBuilder.GetConstraintClauses(model)}"))
            {
                writer.AppendLine($"void Raise({GetParameters(evt)});");
            }
            writer.AppendLine();
        }
    }

    public static void EmitImplementations(CodeWriter writer, MockTypeModel model)
    {
        foreach (var evt in model.Events.Where(NeedsInterface))
        {
            writer.AppendLine($"void {GetInterfaceType(model, evt)}.Raise({GetParameters(evt)}) => Raise_{evt.Name}({GetArguments(evt)});");
            writer.AppendLine();
        }
    }
}
