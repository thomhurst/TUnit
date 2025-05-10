using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.TestDataAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class GeneratedDataAnalyzerTests
{
    #region Method

    [Test]
    public async Task Method_Flags_Error()
    {
        var expected = Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
            .WithLocation(0)
            .WithArguments("int", "string");

        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;
                using System.Collections.Generic;
                            
                namespace TUnit.TestProject;
                            
                public class DataSourceGeneratorTests
                {
                    [Test]
                    [{|#0:AutoFixtureGenerator<int>|}]
                    public void GeneratedData_Method(string value)
                    {
                    }
                                
                    public class AutoFixtureGeneratorAttribute<T> : DataSourceGeneratorAttribute<T>
                    {
                        public override IEnumerable<Func<T>> GenerateDataSources(DataGeneratorMetadata metadata)
                        {
                            return [];
                        }
                    }
                }
                """,
				expected
			);
    }

    [Test]
    public async Task Method_Flags_Error2()
    {
        var expected = Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
            .WithLocation(0)
            .WithArguments("int, string", "string, int");

        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;
                using System.Collections.Generic;

                namespace TUnit.TestProject;

                public class DataSourceGeneratorTests
                {
                    [Test]
                    [{|#0:AutoFixtureGenerator<int, string>|}]
                    public void GeneratedData_Method(string value, int value2)
                    {
                    }
                                
                    public class AutoFixtureGeneratorAttribute<T1, T2> : DataSourceGeneratorAttribute<T1, T2>
                    {
                        public override IEnumerable<Func<(T1, T2)>> GenerateDataSources(DataGeneratorMetadata metadata)
                        {
                            return [];
                        }
                    }
                }
                """,
				expected
			);
    }

    [Test]
    public async Task Method_Flags_Error3()
    {
        var expected = Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
            .WithLocation(0)
            .WithArguments("int, string", "string, int");

        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;
                using System.Collections.Generic;

                namespace TUnit.TestProject;

                public class DataSourceGeneratorTests
                {
                    [Test]
                    [{|#0:AutoFixtureGenerator|}]
                    public void GeneratedData_Method(string value, int value2)
                    {
                    }
                                
                    public class AutoFixtureGeneratorAttribute : DataSourceGeneratorAttribute<int, string>
                    {
                        public override IEnumerable<Func<(int, string)>> GenerateDataSources(DataGeneratorMetadata metadata)
                        {
                            return [];
                        }
                    }
                }
                """,
				expected
			);
    }

    [Test]
    public async Task No_Method_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;
                using System.Collections.Generic;

                namespace TUnit.TestProject;

                public class DataSourceGeneratorTests
                {
                    [Test]
                    [{|#0:AutoFixtureGenerator|}]
                    public void GeneratedData_Method(string value, int value2)
                    {
                    }
                                
                    public class AutoFixtureGeneratorAttribute : DataSourceGeneratorAttribute<string, int>
                    {
                        public override IEnumerable<Func<(string, int)>> GenerateDataSources(DataGeneratorMetadata metadata)
                        {
                            return [];
                        }
                    }
                }
                """
            );
    }

    #endregion

    #region Class

    [Test]
    public async Task Class_Flags_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;
                using System.Collections.Generic;
                            
                namespace TUnit.TestProject;
                            
                [{|#0:AutoFixtureGenerator<int>|}]
                public class DataSourceGeneratorTests(string value)
                {
                    [Test]
                    public void GeneratedData_Method()
                    {
                    }
                                
                    public class AutoFixtureGeneratorAttribute<T> : DataSourceGeneratorAttribute<T>
                    {
                        public override IEnumerable<Func<T>> GenerateDataSources(DataGeneratorMetadata metadata)
                        {
                            return [];
                        }
                    }
                }
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                    .WithLocation(0)
                    .WithArguments("int", "string")
            );
    }

    [Test]
    public async Task Class_Flags_Error2()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;
                using System.Collections.Generic;

                namespace TUnit.TestProject;

                [{|#0:AutoFixtureGenerator<int, string>|}]
                public class DataSourceGeneratorTests(string value, int value2)
                {
                    [Test]
                    public void GeneratedData_Method()
                    {
                    }
                                
                    public class AutoFixtureGeneratorAttribute<T1, T2> : DataSourceGeneratorAttribute<T1, T2>
                    {
                        public override IEnumerable<Func<(T1, T2)>> GenerateDataSources(DataGeneratorMetadata metadata)
                        {
                            return [];
                        }
                    }
                }
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                    .WithLocation(0)
                    .WithArguments("int, string", "string, int")
            );
    }

    [Test]
    public async Task Class_Flags_Error3()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;
                using System.Collections.Generic;

                namespace TUnit.TestProject;

                [{|#0:AutoFixtureGenerator|}]
                public class DataSourceGeneratorTests(string value, int value2)
                {
                    [Test]
                    public void GeneratedData_Method()
                    {
                    }
                                
                    public class AutoFixtureGeneratorAttribute : DataSourceGeneratorAttribute<int, string>
                    {
                        public override IEnumerable<Func<(int, string)>> GenerateDataSources(DataGeneratorMetadata metadata)
                        {
                            return [];
                        }
                    }
                }
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                    .WithLocation(0)
                    .WithArguments("int, string", "string, int")
            );
    }

    [Test]
    public async Task No_Class_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;
                using System.Collections.Generic;

                namespace TUnit.TestProject;

                [AutoFixtureGenerator]
                public class DataSourceGeneratorTests(string value, int value2)
                {
                    [Test]
                    public void GeneratedData_Method()
                    {
                    }
                                
                    public class AutoFixtureGeneratorAttribute : DataSourceGeneratorAttribute<string, int>
                    {
                        public override IEnumerable<Func<(string, int)>> GenerateDataSources(DataGeneratorMetadata metadata)
                        {
                            return [];
                        }
                    }
                }
                """
            );
    }

    #endregion

    #region Properties

    [Test]
    public async Task Property_Flags_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;
                using System.Collections.Generic;
                            
                namespace TUnit.TestProject;
                            
                public class DataSourceGeneratorTests()
                {
                    [{|#0:AutoFixtureGenerator<int>|}]
                    public required string MyProperty { get; init; }
                                
                    [Test]
                    public void GeneratedData_Method()
                    {
                    }
                                
                    public class AutoFixtureGeneratorAttribute<T> : DataSourceGeneratorAttribute<T>
                    {
                        public override IEnumerable<Func<T>> GenerateDataSources(DataGeneratorMetadata metadata)
                        {
                            return [];
                        }
                    }
                }
                """,
                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                    .WithLocation(0)
                    .WithArguments("int", "string")
            );
    }

    [Test]
    public async Task No_Property_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;
                using System.Collections.Generic;
                            
                namespace TUnit.TestProject;
                            
                public class DataSourceGeneratorTests()
                {
                    [{|#0:AutoFixtureGenerator<string>|}]
                    public required string MyProperty { get; init; }
                                
                    [Test]
                    public void GeneratedData_Method()
                    {
                    }
                                
                    public class AutoFixtureGeneratorAttribute<T> : DataSourceGeneratorAttribute<T>
                    {
                        public override IEnumerable<Func<T>> GenerateDataSources(DataGeneratorMetadata metadata)
                        {
                            return [];
                        }
                    }
                }
                """
            );
    }

    #endregion
}