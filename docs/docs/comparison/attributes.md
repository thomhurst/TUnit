---
sidebar_position: 1
---

# Attributes

Here are TUnit's equivalent attributes to other test frameworks.

## Test Attributes

| TUnit                  | xUnit    | NUnit            | MSTest           |
| ---------------------- | -------- | ---------------- | ---------------- |
| [Test]                 | [Fact]   | [Test]           | [TestMethod]     |
| [DataDrivenTest]       | [Theory] | [TestCase]       | [DataTestMethod] |
| [DataSourceDrivenTest] | [Theory] | [TestCaseSource] | [DataTestMethod] |
| [CombinativeTest]      | -        | [Combinatorial]  | -                |

## Data Injection Attributes

| TUnit                        | xUnit                             | NUnit            | MSTest        |
| ---------------------------- | --------------------------------- | ---------------- | ------------- |
| [Arguments]                  | [InlineData]                      | [TestCase]       | [DataRow]     |
| [ClassDataSource]            | [ClassData] or `IClassFixture<T>` | [TestCaseSource] | -             |
| [MethodDataSource]           | [MemberData]                      | [TestCaseSource] | [DynamicData] |
| [EnumerableMethodDataSource] | [MemberData]                      | [TestCaseSource] | [DynamicData] |
| [CombinativeValues]          | -                                 | [Values]         | -             |

## Test Control Attributes

| TUnit           | xUnit                                                 | NUnit                | MSTest   |
| --------------- | ----------------------------------------------------- | -------------------- | -------- |
| [Repeat]        | -                                                     | [Repeat]             | -        |
| [Retry]         | -                                                     | [Retry]              | -        |
| [Skip]          | [Fact(Skip="")]                                       | [Ignore]             | [Ignore] |
| [Timeout]       | -                                                     | [TimeOut]            | -        |
| [Explicit]      | -                                                     | [Explicit]           | -        |
| [NotInParallel] | [CollectionDefinition(DisableParallelization = true)] | [LevelOfParallelism] | -        |

## Lifecycle Hook Attributes

| TUnit                           | xUnit                                      | NUnit                              | MSTest               |
| ------------------------------- | ------------------------------------------ | ---------------------------------- | -------------------- |
| [BeforeEachTest]                | `< Constructor >`                          | [SetUp]                            | [TestInitialize]     |
| [AfterEachTest]                 | `IDisposable.Dispose`                      | [TearDown]                         | [TestCleanup]        |
| [BeforeAllTestsInClass]         | `IClassFixture<T>`                         | [OneTimeSetUp]                     | [ClassInitialize]    |
| [AfterAllTestsInClass]          | `IClassFixture<T>` + `IDisposable.Dispose` | [OneTimeTearDown]                  | [ClassCleanup]       |
| [AssemblySetUp]                 | -                                          | [SetUpFixture] + [OneTimeSetUp]    | [AssemblyInitialize] |
| [AssemblyCleanUp]               | -                                          | [SetUpFixture] + [OneTimeTearDown] | [AssemblyCleanup]    |
| [GlobalBeforeEachTestAttribute] | -                                          | -                                  | -                    |
| [GlobalAfterEachTestAttribute]  | -                                          | -                                  | -                    |

## Metadata Attributes

| TUnit      | xUnit                  | NUnit      | MSTest         |
| ---------- | ---------------------- | ---------- | -------------- |
| [Category] | [Trait("Category","")] | [Category] | [TestCategory] |
| [Property] | [Trait]                | [Property] | [TestProperty] |
