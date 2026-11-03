# Generic class and method inference policy

`GenericMethodInferencePolicy` is compiled into the generator (including its Roslyn variants) and the reflection engine. Its ordered table controls declared-source precedence, row scope, and fallback to explicit method types. A separate shared predicate controls duplicate joining from literal-row status and whether the adapter joins inferred sources. The adapters still extract types using Roslyn symbols or reflection; they do not share representations of types or attributes. Each adapter still switches on source kind, so adding a source requires updating both extraction switches.

| Order | Source | Inferred instantiation runs | Duplicate type arguments | Constraint violation beside explicit types |
| --- | --- | --- | --- | --- |
| 1 | Literal `[Arguments]` | Its literal rows | Join the existing instantiation | Report the literal row |
| 2 | Typed data source | Its own source | First source wins; typed `[Arguments<T>]` rows join | Defer source to explicit types |
| 3 | Parameter attribute implementing `IInfersType<T>` | All method sources | First source wins | No declared-source fallback |
| 4 | `[MethodDataSource]` | Its own source | First source wins | Defer source to explicit types |
| 5 | `[GenerateGenericTest]` | All method sources | Earlier inferred instantiation wins | Report invalid explicit types |

Literal arguments and explicit types are boundary phases. The generator handles them outside the declared-source loop because it also pairs class and method arguments. Reflection handles explicit types after its inferred-source loop. The middle three phases in both modes traverse the shared table. Moving either boundary phase requires changing the class/method pairing algorithm as well.

Within a phase, sources retain declaration order. An ordinary typed source or method source inferring an existing instantiation is dropped when explicit method types exist. Without explicit method types, method sources join that instantiation instead; class-source pairing uses `GenericClassInferencePolicy`, described below. Empty or wrongly sized explicit types fail independently; declared sources still defer to them. Literal rows always join. Deferred sources are restored in declaration order when their explicit instantiation already exists.

Without `[GenerateGenericTest]`, reflection closes declared typed sources first and leaves other sources to value-based runtime inference. The generator performs compile-time inference. This policy does not erase that distinction or promise parity for sources whose runtime values have more specific types than their declarations.

The parity matrix in `GenericMethodInferencePolicyTests` executes the same fixtures in both test modes. It covers each source kind, duplicate literal rows, duplicate non-literal sources, inferred types beside explicit types, and constraint fallback. The fixtures assert their inferred types and values; the matrix checks row counts so accidentally running a dropped source is observable.

`GenericClassInferencePolicy` is also compiled into both adapters. It owns class-source priority, row fallback and membership, type-free literal row classification, and the completeness requirement for combined class/method types. Class rows include literal `[Arguments]`, typed data sources and `[MethodDataSource]`; adapters extract their types and keep source declaration order. Equal class types share an instantiation while retaining their constructor rows.

| Order | Class source | Ownership and scope |
| --- | --- | --- |
| 1 | Class rows (literal, typed or member source) | Matching rows supply constructor arguments; duplicate class types retain each row |
| 2 | Method `[Arguments]` on a non-generic method | Infer class types; rows inferring different class types run on their own instantiation |
| 3 | Method data source | Its declared types may infer class types; extraction stays in the adapter |
| 4 | Parameter inference | Supplies remaining class types without owning one literal row |
| 5 | Class `[GenerateGenericTest]` | Supplies explicit class types; an existing class-row instantiation owns matching types |

Literal and explicit class phases remain boundary phases, as with method inference. The shared order controls the intervening class inference phases and precedence comparisons. It describes selection priority, not the order in which reflection evaluates runtime sources or emits metadata.

A literal constructor row is type-free when its bound data parameters contain no class type parameter (an empty row with no bound parameters is also type-free). It supplies constructor values to a class inferred from method rows. A null method row infers `object` only when neither class sources nor explicit class types supply a type. Row membership includes rows matching the instantiation and, where requested, uninferred rows. If no class row matches, literal class rows produce a missing-constructor-row failure; runtime sources fall back to all rows. A matching class row takes precedence over both fallbacks.

A generic method on a generic class needs a complete pair of class and method type arguments. Each concrete class establishes the outer scope; method sources within it follow the same shared method-source table above, including parameter inference, duplicate joining and constraint fallback. The generator collects those sources in table order; reflection resolves methods on each closed class. Type substitution and constraint extraction remain adapter responsibilities.

The common `GenericMethodInferencePolicyTests` matrix covers class literal/typed/member sources, repeated class types, method-derived class types, inferred and uninferred rows, type-free and empty constructor rows, explicit class and method types, missing constructor rows, and combined class/method source selection. `ClassAndMethodParameterInference` also checks that parameter inference beside duplicate explicit method types preserves the cross product of constructor and matrix rows.

This refactoring preserves the existing mixed-source type-free-row limitation tracked by #7390: reflection only separates type-free literal class rows when all class sources are argument rows. Runtime-value inference can also discover types unavailable to the generator. The shared policy does not make those extraction capabilities identical.
