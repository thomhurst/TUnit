namespace TUnit.TestProject

open TUnit.Core

type CustomSkipAttribute() =
    inherit SkipAttribute("Some Reason")
