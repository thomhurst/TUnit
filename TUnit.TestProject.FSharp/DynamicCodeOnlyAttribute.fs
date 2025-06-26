namespace TUnit.TestProject.FSharp

open System.Threading.Tasks
open System.Runtime.CompilerServices
open TUnit.Core

// F# equivalent of DynamicCodeOnlyAttribute.cs

type DynamicCodeOnlyAttribute() =
    inherit SkipAttribute("This test is only supported when dynamic code is available")
    override _.ShouldSkip(context: BeforeTestContext) =
#if NET
        Task.FromResult(not RuntimeFeature.IsDynamicCodeSupported)
#else
        Task.FromResult(false)
#endif
