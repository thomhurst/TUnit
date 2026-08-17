# Attributes

Here are TUnit's equivalent attributes to other test frameworks.

## Test Attributes[​](#test-attributes "Direct link to Test Attributes")

| TUnit   | xUnit     | NUnit             | MSTest            |
| ------- | --------- | ----------------- | ----------------- |
| \[Test] | \[Fact]   | \[Test]           | \[TestMethod]     |
| \[Test] | \[Theory] | \[TestCase]       | \[DataTestMethod] |
| \[Test] | \[Theory] | \[TestCaseSource] | \[DataTestMethod] |
| \[Test] | -         | \[Combinatorial]  | -                 |

## Data Injection Attributes[​](#data-injection-attributes "Direct link to Data Injection Attributes")

| TUnit               | xUnit                              | NUnit             | MSTest         |
| ------------------- | ---------------------------------- | ----------------- | -------------- |
| \[Arguments]        | \[InlineData]                      | \[TestCase]       | \[DataRow]     |
| \[ClassDataSource]  | \[ClassData] or `IClassFixture<T>` | \[TestCaseSource] | -              |
| \[MethodDataSource] | \[MemberData]                      | \[TestCaseSource] | \[DynamicData] |
| \[Matrix]           | -                                  | \[Values]         | -              |

## Test Control Attributes[​](#test-control-attributes "Direct link to Test Control Attributes")

| TUnit            | xUnit                                                  | NUnit                 | MSTest    |
| ---------------- | ------------------------------------------------------ | --------------------- | --------- |
| \[Repeat]        | -                                                      | \[Repeat]             | -         |
| \[Retry]         | -                                                      | \[Retry]              | -         |
| \[Skip]          | \[Fact(Skip="")]                                       | \[Ignore]             | \[Ignore] |
| \[Timeout]       | -                                                      | \[TimeOut]            | -         |
| \[Explicit]      | -                                                      | \[Explicit]           | -         |
| \[NotInParallel] | \[CollectionDefinition(DisableParallelization = true)] | \[LevelOfParallelism] | -         |

## Lifecycle Hook Attributes[​](#lifecycle-hook-attributes "Direct link to Lifecycle Hook Attributes")

| TUnit                    | xUnit                                      | NUnit                                | MSTest                |
| ------------------------ | ------------------------------------------ | ------------------------------------ | --------------------- |
| \[Before(Test)]          | `< Constructor >`                          | \[SetUp]                             | \[TestInitialize]     |
| \[After(Test)]           | `IDisposable.Dispose`                      | \[TearDown]                          | \[TestCleanup]        |
| \[Before(Class)]         | `IClassFixture<T>`                         | \[OneTimeSetUp]                      | \[ClassInitialize]    |
| \[After(Class)]          | `IClassFixture<T>` + `IDisposable.Dispose` | \[OneTimeTearDown]                   | \[ClassCleanup]       |
| \[Before(Assembly)]      | -                                          | \[SetUpFixture] + \[OneTimeSetUp]    | \[AssemblyInitialize] |
| \[After(Assembly)]       | -                                          | \[SetUpFixture] + \[OneTimeTearDown] | \[AssemblyCleanup]    |
| \[Before(TestSession)]   | -                                          | -                                    | -                     |
| \[After(TestSession)]    | -                                          | -                                    | -                     |
| \[Before(TestDiscovery)] | -                                          | -                                    | -                     |
| \[After(TestDiscovery)]  | -                                          | -                                    | -                     |
| \[BeforeEvery(Test)]     | -                                          | -                                    | -                     |
| \[AfterEvery(Test)]      | -                                          | -                                    | -                     |
| \[BeforeEvery(Class)]    | -                                          | -                                    | -                     |
| \[AfterEvery(Class)]     | -                                          | -                                    | -                     |
| \[BeforeEvery(Assembly)] | -                                          | -                                    | -                     |
| \[AfterEvery(Assembly)]  | -                                          | -                                    | -                     |

## Metadata Attributes[​](#metadata-attributes "Direct link to Metadata Attributes")

| TUnit       | xUnit                   | NUnit       | MSTest          |
| ----------- | ----------------------- | ----------- | --------------- |
| \[Category] | \[Trait("Category","")] | \[Category] | \[TestCategory] |
| \[Property] | \[Trait]                | \[Property] | \[TestProperty] |

## Culture-sensitive Attributes[​](#culture-sensitive-attributes "Direct link to Culture-sensitive Attributes")

| TUnit               | xUnit | NUnit                    | MSTest |
| ------------------- | ----- | ------------------------ | ------ |
| \[Culture("en-US")] | -     | \[SetCulture("en-US")]   | -      |
| -                   | -     | \[Culture("en-US")]      | -      |
| -                   | -     | \[SetUICulture("en-US")] | -      |
