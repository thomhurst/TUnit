# Generic method inference policy

`GenericMethodInferencePolicy` is compiled into the generator (including its Roslyn variants) and the reflection engine. Its ordered table controls declared-source precedence, row scope, and fallback to explicit method types. A separate shared predicate controls duplicate joining from literal-row status and whether the adapter joins inferred sources. The adapters still extract types using Roslyn symbols or reflection; they do not share representations of types or attributes. Each adapter still switches on source kind, so adding a source requires updating both extraction switches.

| Order | Source | Inferred instantiation runs | Duplicate type arguments | Constraint violation beside explicit types |
| --- | --- | --- | --- | --- |
| 1 | Literal `[Arguments]` | Its literal rows | Join the existing instantiation | Report the literal row |
| 2 | Typed data source | Its own source | First source wins; typed `[Arguments<T>]` rows join | Defer source to explicit types |
| 3 | Parameter attribute implementing `IInfersType<T>` | All method sources | First source wins | No declared-source fallback |
| 4 | `[MethodDataSource]` | Its own source | First source wins | Defer source to explicit types |
| 5 | `[GenerateGenericTest]` | All method sources | Earlier inferred instantiation wins | Report invalid explicit types |

Literal arguments and explicit types are boundary phases. The generator handles them outside the declared-source loop because it also pairs class and method arguments. Reflection handles explicit types after its inferred-source loop. The middle three phases in both modes traverse the shared table. Moving either boundary phase requires changing the class/method pairing algorithm as well.

Within a phase, sources retain declaration order. An ordinary typed source or method source inferring an existing instantiation is dropped when explicit method types exist. Without explicit method types, method sources join that instantiation instead; class-source pairing remains adapter-specific. Empty or wrongly sized explicit types fail independently; declared sources still defer to them. Literal rows always join. Deferred sources are restored in declaration order when their explicit instantiation already exists.

Without `[GenerateGenericTest]`, reflection closes declared typed sources first and leaves other sources to value-based runtime inference. The generator performs compile-time inference. This policy does not erase that distinction or promise parity for sources whose runtime values have more specific types than their declarations.

The parity matrix in `GenericMethodInferencePolicyTests` executes the same fixtures in both test modes. It covers each source kind, duplicate literal rows, duplicate non-literal sources, inferred types beside explicit types, and constraint fallback. The fixtures assert their inferred types and values; the matrix checks row counts so accidentally running a dropped source is observable.

Generic **class** instantiation, class rows that infer no type arguments, and class/method pairing remain separate policy work in #7451 (parent #7371). This refactoring does not change those rules.
