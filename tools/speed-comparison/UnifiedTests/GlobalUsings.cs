// Framework-specific namespaces
#if TUNIT
global using TUnit.Core;
global using static TUnit.Core.HookType;
global using TUnit.Assertions;
global using TUnit.Assertions.Extensions;
#elif XUNIT3
global using Xunit;
#elif NUNIT
global using NUnit.Framework;
#elif MSTEST
global using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

// Unified attribute aliases for cross-framework compatibility
#if TUNIT
global using TestAttribute = TUnit.Core.TestAttribute;
global using DataDrivenTestAttribute = TUnit.Core.TestAttribute;
global using TestDataAttribute = TUnit.Core.ArgumentsAttribute;
global using TestDataSourceAttribute = TUnit.Core.MethodDataSourceAttribute;
#elif XUNIT3
// xUnit uses Fact for simple tests, Theory for parameterized tests
global using TestAttribute = Xunit.FactAttribute;
global using DataDrivenTestAttribute = Xunit.TheoryAttribute;
global using TestDataAttribute = Xunit.InlineDataAttribute;
global using TestDataSourceAttribute = Xunit.MemberDataAttribute;
#elif NUNIT
global using TestAttribute = NUnit.Framework.TestAttribute;
global using DataDrivenTestAttribute = NUnit.Framework.TestAttribute;
global using TestDataAttribute = NUnit.Framework.TestCaseAttribute;
global using TestDataSourceAttribute = NUnit.Framework.TestCaseSourceAttribute;
global using TestClassAttribute = NUnit.Framework.TestFixtureAttribute;
#elif MSTEST
global using TestAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute;
global using DataDrivenTestAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute;
global using TestDataAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DataRowAttribute;
global using TestDataSourceAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DynamicDataAttribute;
global using TestClassAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute;
#endif

// Empty attribute for frameworks that don't require class-level attributes
#if TUNIT || XUNIT3
[System.AttributeUsage(System.AttributeTargets.Class)]
internal class TestClassAttribute : System.Attribute { }
#endif