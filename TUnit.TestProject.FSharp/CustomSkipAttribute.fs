namespace TUnit.TestProject.FSharp

open TUnit.Core

type CustomSkipAttribute() =
    inherit SkipAttribute("Some Reason")
