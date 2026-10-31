using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using TUnit.Tests.Shared;

namespace TUnit.Core.SourceGenerator.Tests;

[UnconditionalSuppressMessage("SingleFile", "IL3000:Avoid accessing Assembly file path when publishing as a single file")]
internal class ReferencesHelper
{
    public static readonly List<PortableExecutableReference> References =
        TestHostReferences.CreateReferences(
            typeof(Attribute).Assembly,
            typeof(Assert).Assembly,
            typeof(Polyfill).Assembly);

#if NETFRAMEWORK
    // The net472 leg compiles against .NET Framework's mscorlib, which lacks the types behind
    // `required` and `init`. Add this source to compilations whose test source uses them.
    public const string NetFrameworkPolyfills =
        """
        namespace System.Runtime.CompilerServices;

        internal static class IsExternalInit;

        [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
        internal sealed class CompilerFeatureRequiredAttribute(string featureName) : Attribute
        {
            public string FeatureName { get; } = featureName;
        }

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
        internal sealed class RequiredMemberAttribute : Attribute;
        """;
#endif
}
