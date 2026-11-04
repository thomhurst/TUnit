# Generator diagnostics

Each diagnostic ID identifies one rule across TUnit's source generators. The core and assertion generators share a registry, including their legacy ID prefixes.

| ID          | Generator  | Meaning                                                                                  |
| ----------- | ---------- | ---------------------------------------------------------------------------------------- |
| TU0001      | Assertions | An assertion method could not be found.                                                  |
| THG001      | Core       | Hook metadata generation failed.                                                         |
| TUNIT0999   | Core       | Test metadata generation failed.                                                         |
| TUNIT0998   | Core       | Per-class test-source generation failed.                                                 |
| TUnit0000   | Core       | Dynamic test generation failed.                                                          |
| TUNITGEN001 | Core       | AOT converter generation failed with an exception.                                       |
| TUNITGEN002 | Core       | A conversion has a null source or target type; converter generation is skipped.          |
| TUNITGEN003 | Core       | Checking conversion types failed.                                                        |
| TUNITGEN004 | Assertions | A ref struct parameter requires `InlineMethodBody = true` and an inlineable method body. |
| TUNITGEN005 | Core       | A static data-source property's containing type is inaccessible to generated code.       |
| TUNITGEN006 | Assertions | A method marked `[GenerateAssertion]` must be static.                                    |
| TUNITGEN007 | Assertions | A method marked `[GenerateAssertion]` must have at least one parameter.                  |
| TUNITGEN008 | Assertions | A method marked `[GenerateAssertion]` has an unsupported return type.                    |

Previously, assertion rules TUNITGEN006, TUNITGEN007, and TUNITGEN008 used TUNITGEN001, TUNITGEN002, and TUNITGEN003 respectively, colliding with core rules. Update assertion-specific `.editorconfig`, `NoWarn`, and pragma settings to the new IDs. The core IDs and TUNITGEN004 remain unchanged.

For contributors: allocate IDs in `src/TUnit.Core.SourceGenerator/GeneratorDiagnosticIds.cs`, which is compiled into both generators. Never reuse an allocated ID for another rule. The incremental test suite checks that IDs are unique and each registry entry is used by one rule.
