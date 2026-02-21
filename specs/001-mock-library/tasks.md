# Tasks: TUnit.Mock — Source-Generated Mocking Library

**Input**: Design documents from `/specs/001-mock-library/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/public-api.md

**Tests**: Integration tests are included within each user story phase to validate the story is independently functional.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Project Infrastructure)

**Purpose**: Create all 8 project files, wire into solution, verify empty build.

- [x] T001 Create TUnit.Mock/TUnit.Mock.csproj importing Library.props/Library.targets, with PackagePath items for analyzers and source generator DLLs across roslyn4.4/roslyn4.7/roslyn4.14 slots, plus SourceGenerationDebug.props import
- [x] T002 [P] Create TUnit.Mock/TUnit.Mock.props (implicit usings for TUnit.Mock, TUnit.Mock.Arguments) and TUnit.Mock/TUnit.Mock.targets (global Using items when TUnitMockImplicitUsings=true), with TfmSpecificPackageFile entries in csproj
- [x] T003 [P] Create TUnit.Mock.Analyzers/TUnit.Mock.Analyzers.csproj (netstandard2.0, IsRoslynComponent=true, IsPackable=false, EnforceExtendedAnalyzerRules=true, references Microsoft.CodeAnalysis.Common and Microsoft.CodeAnalysis.CSharp)
- [x] T004 [P] Create TUnit.Mock.SourceGenerator/TUnit.Mock.SourceGenerator.csproj (netstandard2.0, IsRoslynComponent=true, IsPackable=false, import Polyfill.targets, references Microsoft.CodeAnalysis.Common, Microsoft.CodeAnalysis.CSharp, Microsoft.Bcl.AsyncInterfaces)
- [x] T005 [P] Create TUnit.Mock.SourceGenerator.Roslyn44/TUnit.Mock.SourceGenerator.Roslyn44.csproj, TUnit.Mock.SourceGenerator.Roslyn47/TUnit.Mock.SourceGenerator.Roslyn47.csproj, TUnit.Mock.SourceGenerator.Roslyn414/TUnit.Mock.SourceGenerator.Roslyn414.csproj — each a one-liner importing ..\Roslyn.props
- [x] T006 [P] Create TUnit.Mock.SourceGenerator.Tests/TUnit.Mock.SourceGenerator.Tests.csproj (import TestProject.props/targets, reference TUnit.Mock.SourceGenerator, add Verify + Verify.TUnit + Microsoft.CodeAnalysis.SourceGenerators.Testing packages)
- [x] T007 [P] Create TUnit.Mock.Analyzers.Tests/TUnit.Mock.Analyzers.Tests.csproj (import TestProject.props/targets, reference TUnit.Mock.Analyzers, add Microsoft.CodeAnalysis.CSharp.Analyzer.Testing package)
- [x] T008 [P] Create TUnit.Mock.Tests/TUnit.Mock.Tests.csproj (import TestProject.props/targets, reference TUnit.Mock with OutputItemType=Analyzer for source generator, reference TUnit via ProjectReference)
- [x] T009 Add all 8 new projects to TUnit.sln with unique GUIDs, verify `dotnet build TUnit.Mock.sln` or full solution builds successfully with empty projects

**Checkpoint**: All projects compile. Solution builds cleanly.

---

## Phase 2: Foundational (Core Runtime Types + Generator Infrastructure)

**Purpose**: Core types and generator infrastructure that ALL user stories depend on. MUST complete before any story phase.

**CRITICAL**: No user story work can begin until this phase is complete.

- [x] T010 [P] Implement MockBehavior enum (Loose=0, Strict=1) in TUnit.Mock/MockBehavior.cs
- [x] T011 [P] Implement Times readonly struct (Once, Never, Exactly, AtLeast, AtMost, Between, with Matches(int actual) method) in TUnit.Mock/Times.cs
- [x] T012 [P] Implement MockVerificationException (with ExpectedCall, ExpectedCount, ActualCount, ActualCalls properties, formatted message) in TUnit.Mock/Exceptions/MockVerificationException.cs
- [x] T013 [P] Implement MockStrictBehaviorException (with UnconfiguredCall property, formatted message) in TUnit.Mock/Exceptions/MockStrictBehaviorException.cs
- [x] T014 [P] Implement IBehavior interface in TUnit.Mock/Setup/Behaviors/IBehavior.cs and ReturnBehavior{T} in TUnit.Mock/Setup/Behaviors/ReturnBehavior{T}.cs and ThrowBehavior in TUnit.Mock/Setup/Behaviors/ThrowBehavior.cs
- [x] T015 [P] Implement IArgumentMatcher and IArgumentMatcher{T} interfaces in TUnit.Mock/Arguments/IArgumentMatcher.cs with Matches(object?) and Describe() methods
- [x] T016 [P] Implement Arg{T} readonly struct with implicit conversion from T and internal IArgumentMatcher storage in TUnit.Mock/Arguments/Arg{T}.cs
- [x] T017 [P] Implement ExactMatcher{T} (equality comparison via EqualityComparer{T}.Default) in TUnit.Mock/Matchers/ExactMatcher{T}.cs
- [x] T018 [P] Implement CallRecord (int MemberId, string MemberName, object?[] Arguments, DateTime Timestamp) as immutable record in TUnit.Mock/Verification/CallRecord.cs
- [x] T019 [P] Implement MethodSetup (int MemberId, IArgumentMatcher[] Matchers, List{IBehavior} Behaviors, atomic CallIndex) with Matches(object?[]) and Execute() methods in TUnit.Mock/Setup/MethodSetup.cs
- [x] T020 Implement MockEngine{T} core engine in TUnit.Mock/MockEngine{T}.cs — setup store (List{MethodSetup} with ReaderWriterLockSlim), call history (ConcurrentQueue{CallRecord}), HandleCall/HandleCallWithReturn methods (iterate setups last-first, match args, execute behavior, record call), AddSetup, GetCallsFor, Reset methods
- [x] T021 [P] Implement EquatableArray{T} (IEquatable wrapper for ImmutableArray with structural equality and GetHashCode) in TUnit.Mock.SourceGenerator/Models/EquatableArray{T}.cs
- [x] T022 [P] Implement MockTypeModel record (FullyQualifiedName, IsInterface, IsAbstract, Namespace, Members as EquatableArray) and MockMemberModel record (Name, ReturnType, Parameters, IsProperty, IsEvent, IsIndexer, RefKind, TypeParameters, Constraints) in TUnit.Mock.SourceGenerator/Models/
- [x] T023 [P] Implement CodeWriter (StringBuilder wrapper with indentation management: OpenBrace, CloseBrace, WriteLine, AppendLine, nested scopes) in TUnit.Mock.SourceGenerator/CodeWriter.cs
- [x] T024 [P] Implement TypeSymbolExtensions (GetFullyQualifiedName, IsNullableAnnotated, GetSmartDefault for nullability-aware defaults, GetAllInterfaceMembers) and MethodSymbolExtensions (GetParameterDirection for ref/out/in, GetGenericConstraints, IsAsync detection) in TUnit.Mock.SourceGenerator/Extensions/
- [x] T025 Implement MemberDiscovery in TUnit.Mock.SourceGenerator/Discovery/MemberDiscovery.cs — enumerate all mockable members of an ITypeSymbol (methods, properties, events, indexers), walk AllInterfaces, detect explicit implementation needs for overlapping signatures, handle default interface methods

**Checkpoint**: Foundation ready. All core types compile. Generator infrastructure in place. User story implementation can begin.

---

## Phase 3: User Story 1 — Create a Mock and Configure Return Values (P1) MVP

**Goal**: Developer can write `var mock = Mock.Of<IFoo>()`, configure `mock.Setup.Method(args).Returns(value)`, pass `mock` as `IFoo`, and get configured return values.

**Independent Test**: Create mock of a simple interface, configure return value, call method, verify returned value matches.

### Implementation for User Story 1

- [x] T026 [P] [US1] Implement IMethodSetup{TReturn} interface (Returns, Throws, Callback, ReturnsSequentially methods returning ISetupChain{TReturn}) in TUnit.Mock/Setup/IMethodSetup{TReturn}.cs, IVoidMethodSetup in TUnit.Mock/Setup/IVoidMethodSetup.cs, and ISetupChain{TReturn} in TUnit.Mock/Setup/ISetupChain{TReturn}.cs
- [x] T027 [P] [US1] Implement concrete MethodSetupBuilder{TReturn} class (implements IMethodSetup{TReturn} and ISetupChain{TReturn}, creates behaviors, registers with MockEngine) in TUnit.Mock/Setup/MethodSetupBuilder{TReturn}.cs
- [x] T028 [US1] Implement Mock.cs static factory class (Of{T}() and Of{T}(MockBehavior) methods) and Mock{T}.cs wrapper (Object property, Setup property, implicit operator T, Reset method) in TUnit.Mock/Mock.cs and TUnit.Mock/Mock{T}.cs
- [x] T029 [US1] Implement MockTypeDiscovery in TUnit.Mock.SourceGenerator/Discovery/MockTypeDiscovery.cs — syntax predicate (IsMockOfInvocation matching Mock.Of{T}() pattern) and semantic transform (resolve IMethodSymbol, extract TypeArguments[0], build MockTypeModel with all members via MemberDiscovery)
- [x] T030 [US1] Implement MockImplBuilder in TUnit.Mock.SourceGenerator/Builders/MockImplBuilder.cs — generate sealed class T_MockImpl implementing all interface methods, routing each call through MockEngine.HandleCall/HandleCallWithReturn, initialize out params with default!, handle void vs return methods
- [x] T031 [US1] Implement MockSetupBuilder (source gen) in TUnit.Mock.SourceGenerator/Builders/MockSetupBuilder.cs — generate T_MockSetup class mirroring each method with Arg{T} parameters, returning IMethodSetup{TReturn} or IVoidMethodSetup, creating MethodSetup with matchers and registering with MockEngine
- [x] T032 [US1] Implement MockFactoryBuilder in TUnit.Mock.SourceGenerator/Builders/MockFactoryBuilder.cs — generate static factory method that creates Mock{T} with T_MockImpl, T_MockSetup wired to shared MockEngine
- [x] T033 [US1] Implement MockGenerator.cs (IIncrementalGenerator entry point) in TUnit.Mock.SourceGenerator/MockGenerator.cs — wire CreateSyntaxProvider with MockTypeDiscovery, Collect + Distinct for deduplication, RegisterSourceOutput calling MockImplBuilder + MockSetupBuilder + MockFactoryBuilder
- [x] T034 [US1] Write integration tests in TUnit.Mock.Tests/BasicMockTests.cs — test: create mock of simple interface, configure Returns with exact args, call method and assert return value; test unconfigured method returns default; test implicit conversion to interface type; test multiple setups (last wins)

**Checkpoint**: MVP functional. `Mock.Of<IFoo>()` works. `.Setup.Method(args).Returns(value)` works. Mocks can be passed as interface instances.

---

## Phase 4: User Story 2 — Verify Method Calls Were Made (P1)

**Goal**: Developer can verify methods were called with expected arguments and call counts, with clear failure messages.

**Independent Test**: Create mock, call methods, verify call counts succeed and fail appropriately.

**Depends on**: US1 (mock creation and call recording)

### Implementation for User Story 2

- [x] T035 [P] [US2] Implement ICallVerification interface (WasCalled(Times), WasNeverCalled) in TUnit.Mock/Verification/ICallVerification.cs and concrete CallVerificationBuilder (queries MockEngine.GetCallsFor, evaluates Times, throws MockVerificationException with descriptive message listing expected vs actual calls) in TUnit.Mock/Verification/CallVerificationBuilder.cs
- [x] T036 [US2] Implement MockVerifyBuilder (source gen) in TUnit.Mock.SourceGenerator/Builders/MockVerifyBuilder.cs — generate T_MockVerify class mirroring each method with Arg{T} parameters, returning ICallVerification, creating matchers and querying call history
- [x] T037 [US2] Wire MockVerifyBuilder output into MockGenerator.cs, add Verify property to Mock{T}.cs returning generated T_MockVerify, update MockFactoryBuilder to construct T_MockVerify
- [x] T038 [US2] Write integration tests in TUnit.Mock.Tests/VerificationTests.cs — test: verify once succeeds when called once; verify fails with descriptive message when count wrong; verify never succeeds when not called; verify AtLeast/AtMost/Between; verify with exact args

**Checkpoint**: Full setup + verify cycle works. Failure messages are clear and descriptive.

---

## Phase 5: User Story 3 — Argument Matchers (P1)

**Goal**: Developer can use Arg.Any{T}(), Arg.Is{T}(predicate), Arg.Capture{T}() in setup and verification.

**Independent Test**: Configure with matchers, verify matching/non-matching behavior.

**Depends on**: US1 (setup infrastructure), US2 (verification infrastructure)

### Implementation for User Story 3

- [x] T039 [P] [US3] Implement all built-in matchers in TUnit.Mock/Matchers/: AnyMatcher{T}.cs (always true), PredicateMatcher{T}.cs (Func{T,bool}), NullMatcher{T}.cs, NotNullMatcher{T}.cs, CaptureMatcher{T}.cs (stores values in thread-safe list, always matches)
- [x] T040 [P] [US3] Implement ArgCapture{T} (Values as IReadOnlyList{T}, Latest property) in TUnit.Mock/Arguments/ArgCapture{T}.cs
- [x] T041 [US3] Implement Arg static factory class in TUnit.Mock/Arguments/Arg.cs — Any{T}(), Is{T}(T), Is{T}(Func{T,bool}), IsNull{T}(), IsNotNull{T}(), Capture{T}(), Out{T}(T), Ref{T}(T) methods returning Arg{T} or ArgCapture{T}
- [x] T042 [US3] Write integration tests in TUnit.Mock.Tests/ArgumentMatcherTests.cs — test: Arg.Any matches all values; Arg.Is(predicate) matches/rejects correctly; Arg.Capture captures all values; mixed matchers and exact values in same call; matchers work in both Setup and Verify

**Checkpoint**: Full matcher suite works in both setup and verification.

---

## Phase 6: User Story 10 — Compile-Time Safety and Diagnostics (P1)

**Goal**: Roslyn analyzers emit TM001 (sealed class) and TM002 (struct) diagnostics. Strongly-typed generated API catches type mismatches at compile time.

**Independent Test**: Write code that violates rules, confirm compiler errors.

**Depends on**: US1 (source generator exists to validate against)

### Implementation for User Story 10

- [x] T043 [P] [US10] Implement Rules.cs in TUnit.Mock.Analyzers/Rules.cs — define DiagnosticDescriptors: TM001 (Cannot mock sealed type, Error), TM002 (Cannot mock value type, Error)
- [x] T044 [P] [US10] Implement SealedClassMockAnalyzer in TUnit.Mock.Analyzers/SealedClassMockAnalyzer.cs — DiagnosticAnalyzer that detects Mock.Of{T}() where T is sealed class, reports TM001
- [x] T045 [P] [US10] Implement StructMockAnalyzer in TUnit.Mock.Analyzers/StructMockAnalyzer.cs — DiagnosticAnalyzer that detects Mock.Of{T}() where T is struct/value type, reports TM002
- [x] T046 [US10] Write analyzer tests in TUnit.Mock.Analyzers.Tests/SealedClassMockAnalyzerTests.cs and TUnit.Mock.Analyzers.Tests/StructMockAnalyzerTests.cs — verify TM001 fires for sealed class, TM002 fires for struct, no diagnostics for interfaces and abstract classes

**Checkpoint**: Compile-time diagnostics prevent misuse. All P1 stories complete.

---

## Phase 7: User Story 4 — Callbacks, Sequential Behaviors, and Async (P2)

**Goal**: Developer can configure callbacks, sequential chained behaviors (.Then()), and async methods with unified .Returns().

**Independent Test**: Configure sequential behaviors, verify correct progression. Configure async mock, verify auto-wrapping.

**Depends on**: US1 (setup infrastructure)

### Implementation for User Story 4

- [x] T047 [P] [US4] Implement CallbackBehavior (executes Action with args) and ComputedReturnBehavior{T} (executes Func returning value) in TUnit.Mock/Setup/Behaviors/
- [x] T048 [US4] Implement sequential behavior chaining in MethodSetupBuilder — .Then() returns new IMethodSetup for next call's behavior, MethodSetup stores List{IBehavior} with atomic index advancement (Interlocked.Increment), last behavior repeats for calls beyond chain length
- [x] T049 [US4] Implement async unified .Returns() in source generator — MockSetupBuilder detects when method returns Task{T}/ValueTask{T}/Task/ValueTask and generates setup methods accepting unwrapped T, generated code wraps in Task.FromResult/new ValueTask/etc.; .Throws() generates faulted task for async methods
- [x] T050 [US4] Write integration tests in TUnit.Mock.Tests/SequentialBehaviorTests.cs (chained .Throws().Then().Returns() for retry), TUnit.Mock.Tests/CallbackTests.cs (callback receives args), TUnit.Mock.Tests/AsyncTests.cs (unified Returns for Task{T}, ValueTask{T}, async Throws)

**Checkpoint**: Callbacks, sequential behaviors, and async all work.

---

## Phase 8: User Story 5 — Properties and Events (P2)

**Goal**: Developer can mock properties (get/set) and raise events programmatically.

**Independent Test**: Configure property getter, verify setter, raise event and confirm subscriber receives it.

**Depends on**: US1 (generator infrastructure)

### Implementation for User Story 5

- [x] T051 [P] [US5] Implement IPropertySetup{T} (Returns, TrackSets) and IPropertyVerification{T} (GetWasCalled, WasSetTo, SetWasCalled) interfaces in TUnit.Mock/Setup/IPropertySetup{T}.cs and TUnit.Mock/Verification/IPropertyVerification{T}.cs
- [x] T052 [US5] Extend MockImplBuilder to generate property implementations (backing field for configured value, getter/setter routing through MockEngine, event add/remove with backing delegate field) in TUnit.Mock.SourceGenerator/Builders/MockImplBuilder.cs
- [x] T053 [US5] Implement MockRaiseBuilder in TUnit.Mock.SourceGenerator/Builders/MockRaiseBuilder.cs — generate T_MockRaise class with one method per event that invokes the backing delegate, wire into Mock{T}.Raise property
- [x] T054 [US5] Extend MockSetupBuilder and MockVerifyBuilder to generate property setup/verify entries (IPropertySetup{T} for setup, IPropertyVerification{T} for verify)
- [x] T055 [US5] Write integration tests in TUnit.Mock.Tests/PropertyTests.cs (getter returns, setter verify) and TUnit.Mock.Tests/EventTests.cs (raise event, subscriber receives)

**Checkpoint**: Properties and events fully functional.

---

## Phase 9: User Story 6 — Strict Mode and Smart Defaults (P2)

**Goal**: Strict mode throws on unconfigured calls. Loose mode returns nullability-aware smart defaults.

**Independent Test**: Create strict mock, call unconfigured method, verify exception. Create loose mock, verify smart defaults.

**Depends on**: US1 (MockEngine)

### Implementation for User Story 6

- [x] T056 [US6] Implement strict mode in MockEngine{T} — when Behavior=Strict and no setup matches, throw MockStrictBehaviorException with formatted message showing the unconfigured call and its arguments
- [x] T057 [US6] Implement nullability-aware smart defaults in source generator — MockImplBuilder inspects NullableAnnotation on return types: non-nullable string→"", non-nullable collections→empty, Task→CompletedTask, nullable→null, value types→default; generate appropriate default return expressions per member
- [x] T058 [US6] Write integration tests in TUnit.Mock.Tests/StrictModeTests.cs (unconfigured call throws, configured call works, error message quality) and TUnit.Mock.Tests/SmartDefaultTests.cs (string returns "", IList returns empty, nullable returns null, Task returns completed)

**Checkpoint**: Both strict and loose modes work correctly with smart defaults.

---

## Phase 10: User Story 7 — out/ref Parameters and Generic Methods (P2)

**Goal**: Developer can mock methods with out/ref params and generic type parameters naturally.

**Independent Test**: Mock TryGet with out param, mock generic Get{T}, verify correct behavior.

**Depends on**: US1 (generator infrastructure)

### Implementation for User Story 7

- [x] T059 [US7] Extend MockImplBuilder to handle out/ref parameters — generate `out T param = default!;` initialization, route out/ref values through setup configuration, support Arg.Out{T}(value) and Arg.Ref{T}(value) in setup matching
- [x] T060 [US7] Extend MockImplBuilder to handle generic methods — generate type parameters with correct constraints (class, struct, new(), notnull, unmanaged, default, allows ref struct), handle generic type arguments in call recording and setup matching
- [x] T061 [US7] Write integration tests in TUnit.Mock.Tests/OutRefTests.cs (TryGet with out, ref parameter modification) and TUnit.Mock.Tests/GenericTests.cs (generic interface, generic method with constraints)

**Checkpoint**: out/ref and generics work naturally.

---

## Phase 11: User Story 8 — Partial Mocks (P3)

**Goal**: Developer can create partial mocks of abstract/concrete classes where unconfigured virtual methods call base.

**Independent Test**: Create partial mock, override one method, verify base is called for others.

**Depends on**: US1 (generator infrastructure)

### Implementation for User Story 8

- [x] T062 [US8] Implement Mock.OfPartial{T}(params object[] constructorArgs) in TUnit.Mock/Mock.cs — factory method for partial mocks
- [x] T063 [US8] Extend MockImplBuilder to handle abstract/concrete classes — generate class that inherits from T, override all virtual/abstract members, call base for non-configured methods (not default value), handle constructor parameters, detect and skip sealed methods
- [x] T064 [US8] Extend MockTypeDiscovery to detect Mock.OfPartial{T}() calls alongside Mock.Of{T}(), set IsPartial flag on MockTypeModel
- [x] T065 [US8] Write integration tests in TUnit.Mock.Tests/PartialMockTests.cs (override one virtual method, base called for others, abstract class support, constructor args passed)

**Checkpoint**: Partial mocks work for abstract and concrete classes.

---

## Phase 12: User Story 9 — Ordered Call Verification (P3)

**Goal**: Developer can verify calls across mocks happened in specific order.

**Independent Test**: Make calls in order, verify sequence passes. Make calls out of order, verify fails.

**Depends on**: US2 (verification infrastructure)

### Implementation for User Story 9

- [x] T066 [US9] Implement Mock.VerifyInOrder(Action) in TUnit.Mock/Mock.cs — captures verification actions, compares call timestamps across all involved mocks, throws MockVerificationException with expected vs actual order on failure
- [x] T067 [US9] Implement ordered verification tracking — add global (static thread-local) call sequence counter, each CallRecord gets a monotonic sequence number, VerifyInOrder compares sequence numbers of matched calls
- [x] T068 [US9] Write integration tests in TUnit.Mock.Tests/OrderedVerificationTests.cs (correct order passes, wrong order fails with message, cross-mock ordering)

**Checkpoint**: Ordered verification works across multiple mocks.

---

## Phase 13: Polish & Cross-Cutting Concerns

**Purpose**: Quality, documentation, packaging, AOT validation.

- [x] T069 Add XML documentation comments on all public types and members across TUnit.Mock/ (Mock, Mock{T}, Arg, Arg{T}, Times, MockBehavior, IArgumentMatcher, IMethodSetup, ICallVerification, exceptions)
- [x] T070 [P] Write snapshot tests in TUnit.Mock.SourceGenerator.Tests/MockGeneratorTests.cs — verify generated output for: simple interface, multi-method interface, generic interface, interface with properties, interface with events, interface inheriting multiple interfaces, abstract class
- [x] T071 [P] Write thread safety tests in TUnit.Mock.Tests/ThreadSafetyTests.cs — concurrent mock usage from multiple threads, concurrent setup and call
- [x] T072 [P] Write error message quality tests in TUnit.Mock.Tests/ErrorMessageTests.cs — verify MockVerificationException messages contain expected call, actual calls, argument values
- [x] T073 [P] Write mock reset tests in TUnit.Mock.Tests/ResetTests.cs — verify Reset clears setups and history, mock works fresh after reset
- [x] T074 Validate NuGet packaging — run `dotnet pack TUnit.Mock` and verify package contains: lib/{tfms}/TUnit.Mock.dll, analyzers/dotnet/cs/TUnit.Mock.Analyzers.dll, analyzers/dotnet/roslyn4.{4,7,14}/cs/TUnit.Mock.SourceGenerator.dll, build and buildTransitive dirs with props/targets
- [ ] T075 Validate AOT compatibility — create minimal test app consuming TUnit.Mock, publish with -p:PublishAot=true --use-current-runtime, verify zero trimming warnings (SC-008) — DEFERRED (requires AOT publishing environment)

---

## Phase 14: User Story 11 — Advanced Verification (P1)

**Goal**: Developer can call `mock.VerifyNoOtherCalls()`, `mock.VerifyAll()`, and access `mock.Invocations` for custom inspection.

**Independent Test**: Create mock, make calls, verify that VerifyNoOtherCalls catches unverified calls and VerifyAll catches uninvoked setups.

**Depends on**: US1 (mock creation), US2 (verification infrastructure)

### Implementation for User Story 11

- [x] T076 [P] [US11] Add `Invocations` property to `Mock<T>` returning `IReadOnlyList<CallRecord>` that exposes the engine's call history publicly in TUnit.Mock/MockOfT.cs
- [x] T077 [P] [US11] Implement verification tracking in MockEngine — add `MarkCallVerified(CallRecord)` method and `GetUnverifiedCalls()` method, update CallVerificationBuilder to mark matched calls as verified when WasCalled/WasNeverCalled succeeds in TUnit.Mock/MockEngine.cs and TUnit.Mock/Verification/CallVerificationBuilder.cs
- [x] T078 [US11] Implement `VerifyNoOtherCalls()` on Mock<T> — queries `MockEngine.GetUnverifiedCalls()`, throws `MockVerificationException` listing unverified calls if any exist, in TUnit.Mock/MockOfT.cs
- [x] T079 [US11] Implement `VerifyAll()` on Mock<T> — queries all registered setups on MockEngine, checks each was invoked at least once, throws `MockVerificationException` listing uninvoked setups in TUnit.Mock/MockOfT.cs and TUnit.Mock/MockEngine.cs
- [x] T080 [US11] Write integration tests in TUnit.Mock.Tests/VerifyNoOtherCallsTests.cs — test: all verified passes, unverified call fails with details, works after Reset
- [x] T081 [US11] Write integration tests in TUnit.Mock.Tests/VerifyAllTests.cs — test: all setups called passes, uninvoked setup fails with details, setup with no calls fails
- [x] T082 [US11] Write integration tests in TUnit.Mock.Tests/InvocationsTests.cs — test: Invocations returns all calls, includes correct method names and args, empty when no calls made

**Checkpoint**: VerifyNoOtherCalls, VerifyAll, and public Invocations all work.

---

## Phase 15: User Story 12 — Advanced Argument Matchers (P1)

**Goal**: Developer can use `Arg.Matches(regex)`, collection matchers (`Arg.Contains`, `Arg.HasCount`, `Arg.IsEmpty`, `Arg.SequenceEquals`), and create custom matchers.

**Independent Test**: Use regex matcher in setup, verify it matches. Use collection matchers. Create and use custom matcher.

**Depends on**: US3 (argument matcher infrastructure)

### Implementation for User Story 12

- [X] T083 [P] [US12] Implement RegexMatcher in TUnit.Mock/Matchers/RegexMatcher.cs — matches string arguments against a System.Text.RegularExpressions.Regex pattern, with Describe() showing the pattern
- [X] T084 [P] [US12] Implement collection matchers in TUnit.Mock/Matchers/: ContainsMatcher<T>.cs (collection contains item), CountMatcher.cs (collection has N elements), EmptyMatcher.cs (collection is empty), SequenceEqualsMatcher<T>.cs (collection matches sequence)
- [X] T085 [P] [US12] Implement public custom matcher API — ensure IArgumentMatcher interface is public and documented, add Arg.Matches<T>(IArgumentMatcher<T>) overload in TUnit.Mock/Arguments/Arg.cs
- [X] T086 [US12] Add Arg.Matches(string pattern) and Arg.Matches(Regex regex) factory methods to TUnit.Mock/Arguments/Arg.cs
- [X] T087 [US12] Add collection matcher factory methods to Arg: Arg.Contains<T>(T item), Arg.HasCount(int n), Arg.IsEmpty<T>(), Arg.SequenceEquals<T>(IEnumerable<T>) in TUnit.Mock/Arguments/Arg.cs
- [X] T088 [US12] Write integration tests in TUnit.Mock.Tests/RegexMatcherTests.cs — test: regex matches, rejects non-matching, works in setup and verify
- [X] T089 [US12] Write integration tests in TUnit.Mock.Tests/CollectionMatcherTests.cs — test: Contains, HasCount, IsEmpty, SequenceEquals in setup and verify
- [X] T090 [US12] Write integration tests in TUnit.Mock.Tests/CustomMatcherTests.cs — test: user-defined matcher works in setup and verify

**Checkpoint**: All advanced matchers work in both setup and verification.

---

## Phase 16: User Story 13 — Multiple Interface Mocking (P2)

**Goal**: Developer can create `Mock.Of<IFoo, IBar>()` producing a single mock that implements both interfaces.

**Independent Test**: Create multi-interface mock, configure and verify members from both interfaces.

**Depends on**: US1 (mock creation), source generator infrastructure

### Implementation for User Story 13

- [X] T091 [US13] Add `Mock.Of<T1, T2>()`, `Mock.Of<T1, T2, T3>()`, and `Mock.Of<T1, T2, T3, T4>()` overloads to TUnit.Mock/Mock.cs returning `Mock<T1>` where the impl class implements all interfaces
- [X] T092 [US13] Extend MockTypeDiscovery to detect multi-type-parameter Mock.Of calls and build a MockTypeModel with AdditionalInterfaces property in TUnit.Mock.SourceGenerator/Discovery/MockTypeDiscovery.cs and TUnit.Mock.SourceGenerator/Models/MockTypeModel.cs
- [X] T093 [US13] Extend MockImplBuilder to generate impl class implementing all interfaces (primary + additional), merging members from all interfaces in TUnit.Mock.SourceGenerator/Builders/MockImplBuilder.cs
- [X] T094 [US13] Extend MockSetupBuilder and MockVerifyBuilder to include members from all interfaces in generated setup/verify classes in TUnit.Mock.SourceGenerator/Builders/MockSetupBuilder.cs and MockVerifyBuilder.cs
- [X] T095 [US13] Write integration tests in TUnit.Mock.Tests/MultipleInterfaceTests.cs — test: create mock of 2 interfaces, configure from both, cast to both, verify calls on both

**Checkpoint**: Multi-interface mocking works.

---

## Phase 17: User Story 14 — Mock Repository & Batch Operations (P2)

**Goal**: Developer can create a `MockRepository`, create mocks through it, and call `repository.VerifyAll()`, `repository.VerifyNoOtherCalls()`, `repository.Reset()`.

**Independent Test**: Create repository with multiple mocks, batch verify, batch reset.

**Depends on**: US11 (VerifyAll/VerifyNoOtherCalls on individual mocks)

### Implementation for User Story 14

- [X] T096 [US14] Implement IMock interface in TUnit.Mock/IMock.cs — common non-generic interface for Mock<T> exposing VerifyAll, VerifyNoOtherCalls, Reset
- [X] T097 [US14] Implement MockRepository class in TUnit.Mock/MockRepository.cs — Of<T>() creates Mock<T> and tracks it, VerifyAll/VerifyNoOtherCalls/Reset iterate all tracked mocks, thread-safe internal list
- [X] T098 [US14] Have Mock<T> implement IMock interface in TUnit.Mock/MockOfT.cs
- [X] T099 [US14] Write integration tests in TUnit.Mock.Tests/MockRepositoryTests.cs — test: batch VerifyAll catches uninvoked setup on any mock, batch VerifyNoOtherCalls catches unverified calls, batch Reset clears all mocks

**Checkpoint**: MockRepository batch operations work.

---

## Phase 18: User Story 15 — Auto-Track Properties (P2)

**Goal**: Developer can call `mock.SetupAllProperties()` or enable auto-tracking so properties act like real properties.

**Independent Test**: Enable auto-track, set property, read back, verify stored value returned.

**Depends on**: US5 (property mocking infrastructure)

### Implementation for User Story 15

- [X] T100 [US15] Add `SetupAllProperties()` method to Mock<T> that enables auto-tracking mode in MockEngine in TUnit.Mock/MockOfT.cs and TUnit.Mock/MockEngine.cs
- [X] T101 [US15] Extend generated property setters in MockImplBuilder to check auto-tracking mode — when enabled, store value in dictionary keyed by member ID, getters return stored value if present (explicit setup still takes precedence) in TUnit.Mock.SourceGenerator/Builders/MockImplBuilder.cs
- [X] T102 [US15] Write integration tests in TUnit.Mock.Tests/AutoTrackPropertyTests.cs — test: set then get returns value, explicit setup overrides auto-track, unset returns smart default, works with multiple properties

**Checkpoint**: Auto-track properties work with explicit setup taking precedence.

---

## Phase 19: User Story 16 — Advanced Event Support (P2)

**Goal**: Developer can mock events with custom delegate types, auto-raise events on method calls, and verify event subscriptions.

**Independent Test**: Mock interface with custom delegate event, raise it, verify subscriber receives args.

**Depends on**: US5 (event infrastructure), source generator

### Implementation for User Story 16

- [X] T103 [US16] Extend MockImplBuilder and MockRaiseBuilder to support events with custom delegate types (Action<T>, Func<T>, user-defined delegates) — detect delegate type, generate appropriate add/remove/raise code in TUnit.Mock.SourceGenerator/Builders/MockImplBuilder.cs and MockRaiseBuilder.cs
- [ ] T104 [US16] Implement auto-raise configuration — add `.Raises(eventName, args)` to setup chain that triggers event raising when the setup matches in TUnit.Mock/Setup/IMethodSetup.cs, MethodSetupBuilder.cs, and MockEngine.cs — DEFERRED (complex plumbing, low priority)
- [X] T105 [US16] Implement event subscription verification — track subscribe/unsubscribe calls in MockEngine, add verification API for checking subscriptions in TUnit.Mock/MockEngine.cs and generated verify classes
- [X] T106 [US16] Write integration tests in TUnit.Mock.Tests/CustomDelegateEventTests.cs — test: custom delegate event, Action<T> event, subscriber receives args
- [ ] T107 [US16] Write integration tests in TUnit.Mock.Tests/AutoRaiseEventTests.cs — test: method call triggers event raise, event args correct — DEFERRED with T104
- [X] T108 [US16] Write integration tests in TUnit.Mock.Tests/EventSubscriptionVerifyTests.cs — test: verify subscription occurred, verify unsubscription

**Checkpoint**: Full advanced event support works.

---

## Phase 20: User Story 17 — Protected Member Mocking (P3)

**Goal**: Developer can set up and verify protected virtual members through an explicit API.

**Independent Test**: Create partial mock, set up protected method return value, verify it works.

**Depends on**: US8 (partial mocks)

### Implementation for User Story 17

- [X] T109 [US17] Extend source generator to detect protected virtual members and include them in MockSetupBuilder/MockVerifyBuilder — protected members are discovered automatically and accessible via the same setup/verify extension methods (no separate Protected sub-API needed since source gen provides direct access)
- [X] T110 [US17] Write integration tests in TUnit.Mock.Tests/ProtectedMemberTests.cs — test: setup protected virtual method return, verify protected method called, works with abstract class

**Checkpoint**: Protected member explicit API works.

---

## Phase 21: User Story 18 — Delegate Mocking (P3)

**Goal**: Developer can create mock delegates with `Mock.OfDelegate<Func<string, int>>()`.

**Independent Test**: Create mock delegate, configure return, invoke, verify call.

**Depends on**: US1 (mock infrastructure)

### Implementation for User Story 18

- [ ] T111 [US18] Implement `Mock.OfDelegate<TDelegate>()` in TUnit.Mock/Mock.cs — DEFERRED (complex delegate type handling)
- [ ] T112 [US18] Extend source generator to detect delegate type arguments — DEFERRED with T111
- [ ] T113 [US18] Write integration tests — DEFERRED with T111

**Checkpoint**: Delegate mocking works.

---

## Phase 22: User Story 19 — Recursive/Auto Mocking (P3)

**Goal**: Unconfigured methods returning mockable interface types automatically return auto-generated mocks.

**Independent Test**: Create mock of IFoo where GetBar() returns IBar, call GetBar() without setup, verify non-null auto-mock returned.

**Depends on**: US1 (mock infrastructure), source generator

### Implementation for User Story 19

- [ ] T114 [US19] Extend source generator for recursive/auto mocking — DEFERRED (requires transitive type discovery)
- [ ] T115 [US19] Extend MockTypeDiscovery for transitive discovery — DEFERRED with T114
- [ ] T116 [US19] Write integration tests — DEFERRED with T114

**Checkpoint**: Recursive/auto mocking works.

---

## Phase 23: Feature Parity Polish

**Purpose**: Update documentation, snapshots, and comparison table after all gaps are filled.

- [X] T117 Update docs/docs/comparison/mocking-frameworks.md feature matrices to reflect all newly implemented features — change "No" to "Yes" for each completed gap
- [X] T118 [P] Update snapshot tests for source generator output changes in TUnit.Mock.SourceGenerator.Tests — all 10 snapshots updated and passing
- [X] T119 [P] Run full test suite across all test projects — TUnit.Mock.Tests (284), TUnit.Mock.Analyzers.Tests (14), TUnit.Mock.SourceGenerator.Tests (10) — all 308 pass
- [X] T120 Validate AOT compatibility with new features — no dynamic usage, no reflection emit, clean Release build

---

## Phase 24: State Machine Mocking (US20 — Beyond Parity P1)

**Goal**: Named states as first-class guards on method setups — enables testing stateful behavior (retry logic, connections, circuit breakers) without mutable closures.

**Independent Test**: `mock.SetState("disconnected"); mock.InState("disconnected", s => { s.Connect().TransitionsTo("connected"); }); conn.Connect(); conn.GetStatus()` returns state-specific values.

### Implementation

- [X] T121 [US20] Add `RequiredState` (string?) and `TransitionTarget` (string?) properties to `MethodSetup` in `TUnit.Mock/Setup/MethodSetup.cs`
- [X] T122 [US20] Add `_currentState` (string?) field, `PendingRequiredState` (string?) scoped property, and `TransitionTo(string?)` method to `MockEngine<T>` in `TUnit.Mock/MockEngine.cs`
- [X] T123 [US20] Update `FindMatchingSetup` in `TUnit.Mock/MockEngine.cs` to skip setups where `RequiredState != null && RequiredState != _currentState`
- [X] T124 [US20] Update `HandleCall`, `HandleCallWithReturn`, `TryHandleCall`, `TryHandleCallWithReturn` in `TUnit.Mock/MockEngine.cs` to apply `TransitionTarget` after behavior executes (call `TransitionTo(setup.TransitionTarget)`)
- [X] T125 [US20] Add `TransitionsTo(string stateName)` to `ISetupChain<TReturn>` and `IVoidSetupChain` interfaces in `TUnit.Mock/Setup/ISetupChain.cs`
- [X] T126 [US20] Implement `TransitionsTo` in `MethodSetupBuilder<TReturn>` (`TUnit.Mock/Setup/MethodSetupBuilder.cs`) and `VoidMethodSetupBuilder` (`TUnit.Mock/Setup/VoidMethodSetupBuilder.cs`) — sets `MethodSetup.TransitionTarget`
- [X] T127 [US20] Add `SetState(string?)` and `InState(string, Action<IMockSetup<T>>)` to `Mock<T>` in `TUnit.Mock/MockOfT.cs` — `InState` sets `Engine.PendingRequiredState`, invokes action, then clears it; `AddSetup` stamps `PendingRequiredState` onto new setups
- [X] T128 [US20] Add state machine integration tests in `TUnit.Mock.Tests/StateMachineTests.cs` — connect/disconnect cycle, state transitions, state-scoped returns, strict mode with states

**Checkpoint**: State machine mocking fully functional. No generator changes required.

---

## Phase 25: Mock Diagnostics (US21 — Beyond Parity P1)

**Goal**: `mock.GetDiagnostics()` returns structured report of unused setups, unmatched calls, and setup coverage ratio.

**Independent Test**: Configure setups, exercise some, call `GetDiagnostics()` — verify `UnusedSetups`, `UnmatchedCalls`, `TotalSetups`, `ExercisedSetups` counts.

### Implementation

- [X] T129 [P] [US21] Create `MockDiagnostics` record in `TUnit.Mock/Diagnostics/MockDiagnostics.cs` — `sealed record MockDiagnostics(IReadOnlyList<SetupInfo> UnusedSetups, IReadOnlyList<CallRecord> UnmatchedCalls, int TotalSetups, int ExercisedSetups)`
- [X] T130 [P] [US21] Create `SetupInfo` record in `TUnit.Mock/Diagnostics/SetupInfo.cs` — `sealed record SetupInfo(int MemberId, string MemberName, string[] MatcherDescriptions, int InvokeCount)`
- [X] T131 [US21] Add `Describe()` method to `IArgumentMatcher` interface and all implementations (`AnyMatcher`, `ExactMatcher`, `PredicateMatcher`, `NullMatcher`, `NotNullMatcher`, `CaptureMatcher`, `RegexMatcher`, `ContainsMatcher`, `CountMatcher`, `EmptyMatcher`, `SequenceEqualsMatcher`) in `TUnit.Mock/Arguments/` — returns human-readable description for diagnostics
- [X] T132 [US21] Track unmatched calls in `MockEngine<T>` — when `FindMatchingSetup` returns `(false, null, null)` in loose mode, mark the `CallRecord` with `IsUnmatched = true` flag in `TUnit.Mock/MockEngine.cs`
- [X] T133 [US21] Add `GetDiagnostics()` method to `MockEngine<T>` in `TUnit.Mock/MockEngine.cs` — aggregates `GetSetups()` for unused (InvokeCount==0) and total/exercised counts, filters `_callHistory` for unmatched calls
- [X] T134 [US21] Add `GetDiagnostics()` to `Mock<T>` in `TUnit.Mock/MockOfT.cs` — delegates to `Engine.GetDiagnostics()`
- [X] T135 [US21] Add diagnostics integration tests in `TUnit.Mock.Tests/DiagnosticsTests.cs` — unused setup detection, unmatched call tracking, coverage ratio, reset clears diagnostics

**Checkpoint**: Mock diagnostics fully functional. No generator changes required.

---

## Phase 26: Async Verification — TUnit.Mock.Assertions Bridge (US22 — Beyond Parity P2)

**Goal**: New `TUnit.Mock.Assertions` NuGet package that bridges TUnit.Mock + TUnit.Assertions, enabling `await Assert.That(mock.Verify!.Method()).WasCalled(Times.Once)`. Zero changes to TUnit.Mock itself.

**Independent Test**: `await Assert.That(mock.Verify!.Add(1, 2)).WasCalled(Times.Once)` passes; `await Assert.That(mock.Verify!.Reset()).WasNeverCalled()` passes; works inside `Assert.Multiple`.

### Implementation

- [X] T136 [US22] Create `TUnit.Mock.Assertions/TUnit.Mock.Assertions.csproj` — multi-target netstandard2.0;net8.0;net9.0;net10.0, references TUnit.Mock (ProjectReference) + TUnit.Assertions (ProjectReference), import Library.props/Library.targets
- [X] T137 [US22] Create `WasCalledAssertion` class in `TUnit.Mock.Assertions/WasCalledAssertion.cs` — extends `Assertion<ICallVerification>`, wraps sync `WasCalled(Times)` in try/catch converting `MockVerificationException` to `AssertionResult.Failed`
- [X] T138 [P] [US22] Create `WasNeverCalledAssertion` class in `TUnit.Mock.Assertions/WasNeverCalledAssertion.cs` — extends `Assertion<ICallVerification>`, wraps sync `WasNeverCalled()` in try/catch
- [X] T139 [US22] Create `MockAssertionExtensions` static class in `TUnit.Mock.Assertions/MockAssertionExtensions.cs` — extension methods on `IAssertionSource<ICallVerification>` for `WasCalled(Times)` and `WasNeverCalled()`
- [X] T140 [P] [US22] Skipped — PropertyAssertionExtensions not needed; generated verify surface returns ICallVerification for property getters/setters already covered by MockAssertionExtensions
- [X] T141 [US22] Add TUnit.Mock.Assertions project to solution, add ProjectReference in TUnit.Mock.Tests.csproj, verify build passes on all TFMs
- [X] T142 [US22] Add async verification tests in `TUnit.Mock.Tests/AsyncVerificationTests.cs` — Assert.That WasCalled, WasNeverCalled, Assert.Multiple integration, property verification assertions

**Checkpoint**: Async verification bridge fully functional. TUnit.Mock has zero dependency on TUnit.Assertions.

---

## Phase 27: Strongly-Typed Callbacks (US23 — Beyond Parity P1)

**Goal**: Source generator emits per-method wrapper structs with typed `Returns(Func<T1, T2, TReturn>)`, `Callback(Action<T1, T2>)`, and `Throws(Func<T1, T2, Exception>)` overloads for methods with 1-8 parameters.

**Independent Test**: `mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>()).Returns((int a, int b) => a + b)` compiles and works; `mock.Setup.Add(...).Callback((int a, int b) => log.Add(a))` calls typed lambda.

### Implementation

- [X] T143 [US23] Update `MockSetupBuilder.cs` in `TUnit.Mock.SourceGenerator/Builders/MockSetupBuilder.cs` — for each method with 1-8 non-out parameters, generate a readonly wrapper struct `{MethodName}_Setup` (or `{MethodName}_VoidSetup` for void) that forwards all `IMethodSetup<TReturn>` methods to inner builder and adds typed overloads: `Returns(Func<T1,...,TReturn>)`, `Callback(Action<T1,...>)`, `Throws(Func<T1,...,Exception>)` — each converting typed delegate to `object?[]`-based delegate
- [X] T144 [US23] Update setup extension methods in `MockSetupBuilder.cs` to return the wrapper struct type instead of `IMethodSetup<TReturn>` / `IVoidMethodSetup` for methods with 1-8 parameters — wrapper struct implements the interface so existing code still works
- [X] T145 [US23] Update snapshot `.verified.txt` files in `TUnit.Mock.SourceGenerator.Tests/Snapshots/` — regenerate all snapshots to reflect wrapper struct generation
- [X] T146 [US23] Add strongly-typed callback/returns tests in `TUnit.Mock.Tests/TypedCallbackTests.cs` — typed Returns with computed value, typed Callback with side effect, typed Throws with argument-dependent exception, void method typed callback, chaining typed with Then()

**Checkpoint**: Strongly-typed callbacks fully functional. Source generator produces wrapper structs. All existing tests still pass.

---

## Phase 28: Beyond-Parity Final Validation

**Purpose**: Cross-cutting validation of all 4 beyond-parity features together.

- [X] T147 [P] Run full TUnit.Mock.Tests suite — 396 tests pass (existing + state machine, diagnostics, async verification, typed callback tests)
- [X] T148 [P] Run TUnit.Mock.Analyzers.Tests — 88 tests pass (22 x 4 TFMs)
- [X] T149 [P] Run TUnit.Mock.SourceGenerator.Tests — all 40 snapshots match (including updated wrapper struct output)
- [X] T150 Validate AOT compatibility — `dotnet build TUnit.Mock/TUnit.Mock.csproj -c Release` clean on all TFMs, no dynamic usage in new code
- [X] T151 Build pipeline validation — `dotnet build TUnit.Pipeline/TUnit.Pipeline.csproj -c Release` succeeds

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Foundational — **MVP target**
- **US2 (Phase 4)**: Depends on US1 (mock creation + call recording)
- **US3 (Phase 5)**: Depends on US1 (setup infrastructure) and US2 (verify infrastructure)
- **US10 (Phase 6)**: Depends on US1 (generator exists to detect misuse)
- **US4 (Phase 7)**: Depends on US1 (setup infrastructure)
- **US5 (Phase 8)**: Depends on US1 (generator infrastructure)
- **US6 (Phase 9)**: Depends on US1 (MockEngine)
- **US7 (Phase 10)**: Depends on US1 (generator infrastructure)
- **US8 (Phase 11)**: Depends on US1 (generator infrastructure)
- **US20 (Phase 24)**: State Machine — Depends on Phases 1-23 complete (runtime-only, no generator changes)
- **US21 (Phase 25)**: Diagnostics — Depends on Phases 1-23 complete (runtime-only, no generator changes). Independent of US20.
- **US22 (Phase 26)**: Async Verification — Depends on Phases 1-23 complete. New project, independent of US20/US21.
- **US23 (Phase 27)**: Typed Callbacks — Depends on Phases 1-23 complete. Generator changes, independent of US20/US21/US22.
- **Phase 28**: Final Validation — Depends on all of US20-US23 complete
- **US9 (Phase 12)**: Depends on US2 (verification infrastructure)
- **Polish (Phase 13)**: Depends on all desired user stories
- **US11 (Phase 14)**: Depends on US2 (verification) — VerifyNoOtherCalls/VerifyAll
- **US12 (Phase 15)**: Depends on US3 (matcher infrastructure)
- **US13 (Phase 16)**: Depends on US1 (mock creation + source gen)
- **US14 (Phase 17)**: Depends on US11 (VerifyAll/VerifyNoOtherCalls)
- **US15 (Phase 18)**: Depends on US5 (property infrastructure)
- **US16 (Phase 19)**: Depends on US5 (event infrastructure)
- **US17 (Phase 20)**: Depends on US8 (partial mocks)
- **US18 (Phase 21)**: Depends on US1 (mock infrastructure)
- **US19 (Phase 22)**: Depends on US1 (mock infrastructure + source gen)
- **Feature Parity Polish (Phase 23)**: Depends on all feature gap stories

### User Story Independence

After US1 (the MVP), these stories can proceed **in parallel**:

```
                  ┌─> US2 (Verify) ──> US3 (Matchers) ──> US12 (Adv. Matchers)
                  │                ──> US9 (Ordered)
                  │                ──> US11 (VerifyNoOther/All) ──> US14 (Repository)
US1 (MVP) ────────┼─> US4 (Callbacks/Async)
                  ├─> US5 (Props/Events) ──> US15 (Auto-Track)
                  │                      ──> US16 (Adv. Events)
                  ├─> US6 (Strict/Defaults)
                  ├─> US7 (out/ref/Generics)
                  ├─> US8 (Partial Mocks) ──> US17 (Protected)
                  ├─> US10 (Analyzers)
                  ├─> US13 (Multi-Interface)
                  ├─> US18 (Delegates)
                  └─> US19 (Recursive)
```

### Within Each User Story

- Setup interfaces/types before implementation classes
- Source generator builders before integration wiring
- Integration tests last (validate story works end-to-end)

### Parallel Opportunities

- **Phase 1**: T002-T008 all parallel (different files)
- **Phase 2**: T010-T019 and T021-T025 all parallel (different files); T020 depends on T014-T019
- **Phase 3**: T026-T027 parallel; T029-T032 sequential (builder depends on builder)
- **Phase 4-12**: Within each phase, tasks marked [P] can run in parallel
- **Cross-phase**: After US1, US4/US5/US6/US7/US8/US10 can all run in parallel

---

## Parallel Example: Phase 2 (Foundational)

```
# Launch all independent foundational types in parallel:
Agent 1: T010 MockBehavior + T011 Times + T012 MockVerificationException + T013 MockStrictBehaviorException
Agent 2: T014 IBehavior + behaviors + T015 IArgumentMatcher + T016 Arg{T} + T017 ExactMatcher
Agent 3: T018 CallRecord + T019 MethodSetup
Agent 4: T021 EquatableArray + T022 MockTypeModel + T023 CodeWriter + T024 Extensions

# Then sequentially (depend on above):
Agent 1: T020 MockEngine{T}
Agent 2: T025 MemberDiscovery
```

## Parallel Example: After US1 MVP

```
# All these stories can launch simultaneously:
Agent 1: US2 (Verify) → then US3 (Matchers) → then US9 (Ordered)
Agent 2: US4 (Callbacks/Async)
Agent 3: US5 (Properties/Events)
Agent 4: US6 (Strict/Defaults) + US7 (out/ref/Generics)
Agent 5: US8 (Partial Mocks)
Agent 6: US10 (Analyzers)
```

## Parallel Example: Beyond-Parity Features (Phases 24-27)

```
# All 4 beyond-parity features are independent — can launch simultaneously:
Agent 1: US20 (State Machine) — runtime-only changes to MockEngine + MethodSetup
Agent 2: US21 (Diagnostics) — runtime-only, new Diagnostics/ folder + MockEngine additions
Agent 3: US22 (Async Verification) — entirely new TUnit.Mock.Assertions project
Agent 4: US23 (Typed Callbacks) — generator changes to MockSetupBuilder + snapshots

# After all 4 complete → Phase 28 final validation
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T009)
2. Complete Phase 2: Foundational (T010-T025)
3. Complete Phase 3: User Story 1 (T026-T034)
4. **STOP and VALIDATE**: `Mock.Of<IFoo>()` works, `.Setup.Method(args).Returns(value)` works
5. This alone is a usable, valuable library for basic stubbing

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. US1 → Basic mocking works → **MVP!**
3. US2 → Verification works → Most tests can now be written
4. US3 → Matchers work → Real-world test patterns possible
5. US10 → Analyzers → Compile-time safety
6. US4-US9 → Advanced features → Feature parity with NSubstitute/Moq
7. Polish → Production ready
8. US20 → State machine mocking → Beyond parity (no other framework has this)
9. US21 → Mock diagnostics → Beyond parity (unused setup detection)
10. US22 → Async verification → TUnit assertion integration (separate package)
11. US23 → Typed callbacks → Source-gen exclusive typed lambdas

### Task Counts

| Phase | Story | Tasks | Parallel |
|-------|-------|-------|----------|
| Phase 1 | Setup | 9 | 7 |
| Phase 2 | Foundational | 16 | 14 |
| Phase 3 | US1 (P1) | 9 | 2 |
| Phase 4 | US2 (P1) | 4 | 1 |
| Phase 5 | US3 (P1) | 4 | 2 |
| Phase 6 | US10 (P1) | 4 | 3 |
| Phase 7 | US4 (P2) | 4 | 1 |
| Phase 8 | US5 (P2) | 5 | 1 |
| Phase 9 | US6 (P2) | 3 | 0 |
| Phase 10 | US7 (P2) | 3 | 0 |
| Phase 11 | US8 (P3) | 4 | 0 |
| Phase 12 | US9 (P3) | 3 | 0 |
| Phase 13 | Polish | 7 | 4 |
| Phase 14 | US11 - Adv. Verify (P1) | 7 | 2 |
| Phase 15 | US12 - Adv. Matchers (P1) | 8 | 3 |
| Phase 16 | US13 - Multi-Interface (P2) | 5 | 0 |
| Phase 17 | US14 - Repository (P2) | 4 | 0 |
| Phase 18 | US15 - Auto-Track (P2) | 3 | 0 |
| Phase 19 | US16 - Adv. Events (P2) | 6 | 0 |
| Phase 20 | US17 - Protected (P3) | 2 | 0 |
| Phase 21 | US18 - Delegates (P3) | 3 | 0 |
| Phase 22 | US19 - Recursive (P3) | 3 | 0 |
| Phase 23 | Feature Parity Polish | 4 | 2 |
| Phase 24 | US20 - State Machine (P1) | 8 | 0 |
| Phase 25 | US21 - Diagnostics (P1) | 7 | 2 |
| Phase 26 | US22 - Async Verify (P2) | 7 | 2 |
| Phase 27 | US23 - Typed Callbacks (P1) | 4 | 0 |
| Phase 28 | Beyond-Parity Validation | 5 | 3 |
| **Total** | | **151** | **49** |

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks
- [Story] label maps task to specific user story for traceability
- Each user story is independently completable and testable after its phase
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- All file paths are relative to repository root (C:\git\TUnit\)
- Generator builders are sequential within US1 (each builder depends on previous patterns)
- After US1 MVP, maximum parallelism is possible (6 concurrent story streams)
- **Beyond-parity features** (Phases 24-28): All 4 features are independent — US20-US23 can run in parallel
- **TUnit.Mock.Assertions** (US22) is a separate project — TUnit.Mock has ZERO dependency on TUnit.Assertions
- **Typed callbacks** (US23) is the only feature requiring generator changes — all others are runtime-only
