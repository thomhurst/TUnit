# Attributes

Here are TUnit's equivalent attributes to other test frameworks.

## Test Attributes

| TUnit  | xUnit    | NUnit            | MSTest           |
| ------ | -------- | ---------------- | ---------------- |
| [Test] | [Fact]   | [Test]           | [TestMethod]     |
| [Test] | [Theory] | [TestCase]       | [DataTestMethod] |
| [Test] | [Theory] | [TestCaseSource] | [DataTestMethod] |
| [Test] | -        | [Combinatorial]  | -                |

## Data Injection Attributes

| TUnit                        | xUnit                             | NUnit            | MSTest        |
| ---------------------------- | --------------------------------- | ---------------- | ------------- |
| [Arguments]                  | [InlineData]                      | [TestCase]       | [DataRow]     |
| [ClassDataSource]            | [ClassData] or `IClassFixture<T>` | [TestCaseSource] | -             |
| [MethodDataSource]           | [MemberData]                      | [TestCaseSource] | [DynamicData] |
| [Matrix]                     | -                                 | [Values]         | -             |

## Test Control Attributes

| TUnit           | xUnit                                                 | NUnit                | MSTest   | Notes | 
| --------------- | ----------------------------------------------------- | -------------------- | -------- | -------------- |
| [Repeat]        | -                                                     | [Repeat]             | -        |
| [Retry]         | -                                                     | [Retry]              | -        |
| [Skip]          | [Fact(Skip="")]                                       | [Ignore]             | [Ignore] |
| - [^1]          | [Fact(SkipWhen/SkipUnless = ...)][^2]                 | -                    | -        | Dynamic Skip |
| [Timeout]       | [Fact(Timeout = 1000)][^2]                            | [TimeOut]            | -        |
| [Explicit]      | [Fact(Explicit = true)][^2]                           | [Explicit]           | -        |
| [NotInParallel] | [CollectionDefinition(DisableParallelization = true)] | [LevelOfParallelism] | -        |

[^1]: Inherit from `SkipAttribute`
[^2]: Introduced in **xUnit.v3**

## Lifecycle Hook Attributes

| TUnit                   | xUnit                                      | NUnit                              | MSTest               |
| ----------------------- | ------------------------------------------ | ---------------------------------- | -------------------- |
| [Before(Test)]          | `< Constructor >`                          | [SetUp]                            | [TestInitialize]     |
| [After(Test)]           | `IDisposable.Dispose`                      | [TearDown]                         | [TestCleanup]        |
| [Before(Class)]         | `IClassFixture<T>`                         | [OneTimeSetUp]                     | [ClassInitialize]    |
| [After(Class)]          | `IClassFixture<T>` + `IDisposable.Dispose` | [OneTimeTearDown]                  | [ClassCleanup]       |
| [Before(Assembly)]      | -                                          | [SetUpFixture] + [OneTimeSetUp]    | [AssemblyInitialize] |
| [After(Assembly)]       | -                                          | [SetUpFixture] + [OneTimeTearDown] | [AssemblyCleanup]    |
| [Before(TestSession)]   | -                                          | -                                  | -                    |
| [After(TestSession)]    | -                                          | -                                  | -                    |
| [Before(TestDiscovery)] | -                                          | -                                  | -                    |
| [After(TestDiscovery)]  | -                                          | -                                  | -                    |
| [BeforeEvery(Test)]     | -                                          | -                                  | -                    |
| [AfterEvery(Test)]      | -                                          | -                                  | -                    |
| [BeforeEvery(Class)]    | -                                          | -                                  | -                    |
| [AfterEvery(Class)]     | -                                          | -                                  | -                    |
| [BeforeEvery(Assembly)] | -                                          | -                                  | -                    |
| [AfterEvery(Assembly)]  | -                                          | -                                  | -                    |

## Metadata Attributes

| TUnit      | xUnit                  | NUnit      | MSTest         |
| ---------- | ---------------------- | ---------- | -------------- |
| [Category] | [Trait("Category","")] | [Category] | [TestCategory] |
| [Property] | [Trait]                | [Property] | [TestProperty] |

## Culture-sensitive Attributes

| TUnit              | xUnit                                                   | NUnit                    | MSTest | Notes |
|--------------------|---------------------------------------------------------|--------------------------|--------|--------------|
| [Culture("en-US")] | [CulturedFact("en-US")] / [CulturedTheory("en-US")][^3] | [SetCulture("en-US")]    | -      | Sets thread culture |
| [Culture("en-US")] | -                                                       | [SetUICulture("en-US")]  | -      | Sets thread UI culture |
| -                  | -                                                       | [Culture("en-US")]       | -      | Restricts test to culture(s) |

[^3]: Introduced in **xUnit.v3**. Accept one or more cultures; the test is run once per specified culture.
