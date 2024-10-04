using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.GeneratedDataAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class GeneratedDataAnalyzerTests
{
    #region Method

    [Test]
    public async Task Method_Flags_Error()
    {
        const string text = """
                            using TUnit.Core;
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
                                    public override IEnumerable<T> GenerateDataSources(DataGeneratorMetadata metadata)
                                    {
                                        return [];
                                    }
                                }
                            }
                            """;
        
        var expected = Verifier.Diagnostic(Rules.WrongArgumentTypeTestDataSource)
            .WithLocation(0)
            .WithMessage("The data source type `int` doesn't match the parameter type `string`");

        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
    
    [Test]
    public async Task Method_Flags_Error2()
    {
        const string text = """
                            using TUnit.Core;
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
                                    public override IEnumerable<(T1, T2)> GenerateDataSources(DataGeneratorMetadata metadata)
                                    {
                                        return [];
                                    }
                                }
                            }
                            """;
        
        var expected = Verifier.Diagnostic(Rules.WrongArgumentTypeTestDataSource)
            .WithLocation(0)
            .WithMessage("The data source type `int, string` doesn't match the parameter type `string, int`");

        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
    
    [Test]
    public async Task Method_Flags_Error3()
    {
        const string text = """
                            using TUnit.Core;
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
                                    public override IEnumerable<(int, string)> GenerateDataSources(DataGeneratorMetadata metadata)
                                    {
                                        return [];
                                    }
                                }
                            }
                            """;
        
        var expected = Verifier.Diagnostic(Rules.WrongArgumentTypeTestDataSource)
            .WithLocation(0)
            .WithMessage("The data source type `int, string` doesn't match the parameter type `string, int`");

        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
    
    [Test]
    public async Task No_Method_Error()
    {
        const string text = """
                            using TUnit.Core;
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
                                    public override IEnumerable<(string, int)> GenerateDataSources(DataGeneratorMetadata metadata)
                                    {
                                        return [];
                                    }
                                }
                            }
                            """;

        await Verifier.VerifyAnalyzerAsync(text);
    }
    
    #endregion

    #region Class

    [Test]
    public async Task Class_Flags_Error()
    {
        const string text = """
                            using TUnit.Core;
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
                                    public override IEnumerable<T> GenerateDataSources(DataGeneratorMetadata metadata)
                                    {
                                        return [];
                                    }
                                }
                            }
                            """;
        
        var expected = Verifier.Diagnostic(Rules.WrongArgumentTypeTestDataSource)
            .WithLocation(0)
            .WithMessage("The data source type `int` doesn't match the parameter type `string`");

        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
    
    [Test]
    public async Task Class_Flags_Error2()
    {
        const string text = """
                            using TUnit.Core;
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
                                    public override IEnumerable<(T1, T2)> GenerateDataSources(DataGeneratorMetadata metadata)
                                    {
                                        return [];
                                    }
                                }
                            }
                            """;
        
        var expected = Verifier.Diagnostic(Rules.WrongArgumentTypeTestDataSource)
            .WithLocation(0)
            .WithMessage("The data source type `int, string` doesn't match the parameter type `string, int`");

        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
    
    [Test]
    public async Task Class_Flags_Error3()
    {
        const string text = """
                            using TUnit.Core;
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
                                    public override IEnumerable<(int, string)> GenerateDataSources(DataGeneratorMetadata metadata)
                                    {
                                        return [];
                                    }
                                }
                            }
                            """;
        
        var expected = Verifier.Diagnostic(Rules.WrongArgumentTypeTestDataSource)
            .WithLocation(0)
            .WithMessage("The data source type `int, string` doesn't match the parameter type `string, int`");

        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
    
    [Test]
    public async Task No_Class_Error()
    {
        const string text = """
                            using TUnit.Core;
                            using System.Collections.Generic;

                            namespace TUnit.TestProject;

                            [{|#0:AutoFixtureGenerator|}]
                            public class DataSourceGeneratorTests(string value, int value2)
                            {
                                [Test]
                                public void GeneratedData_Method()
                                {
                                }
                                
                                public class AutoFixtureGeneratorAttribute : DataSourceGeneratorAttribute<string, int>
                                {
                                    public override IEnumerable<(string, int)> GenerateDataSources(DataGeneratorMetadata metadata)
                                    {
                                        return [];
                                    }
                                }
                            }
                            """;

        await Verifier.VerifyAnalyzerAsync(text);
    }

    #endregion

    #region Properties

    [Test]
    public async Task Property_Flags_Error()
    {
        const string text = """
                            using TUnit.Core;
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
                                    public override IEnumerable<T> GenerateDataSources(DataGeneratorMetadata metadata)
                                    {
                                        return [];
                                    }
                                }
                            }
                            """;
        
        var expected = Verifier.Diagnostic(Rules.WrongArgumentTypeTestDataSource)
            .WithLocation(0)
            .WithMessage("The data source type `int` doesn't match the parameter type `string`");

        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
    
    [Test]
    public async Task Property_Flags_Error2()
    {
        const string text = """
                            using TUnit.Core;
                            using System.Collections.Generic;

                            namespace TUnit.TestProject;

                            public class DataSourceGeneratorTests()
                            {
                                [{|#0:AutoFixtureGenerator<string, int>|}]
                                public required string MyProperty { get; init; }
                                
                                [Test]
                                public void GeneratedData_Method()
                                {
                                }
                                
                                public class AutoFixtureGeneratorAttribute<T1, T2> : DataSourceGeneratorAttribute<T1, T2>
                                {
                                    public override IEnumerable<(T1, T2)> GenerateDataSources(DataGeneratorMetadata metadata)
                                    {
                                        return [];
                                    }
                                }
                            }
                            """;
        
        var expected = Verifier.Diagnostic(Rules.WrongArgumentTypeTestDataSource)
            .WithLocation(0)
            .WithMessage("The data source type `string, int` doesn't match the parameter type `string`");

        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
    
    [Test]
    public async Task Property_Flags_Error3()
    {
        const string text = """
                            using TUnit.Core;
                            using System.Collections.Generic;

                            namespace TUnit.TestProject;

                            public class DataSourceGeneratorTests
                            {
                                [{|#0:AutoFixtureGenerator|}]
                                public required int MyProperty { get; init; }
                                
                                [Test]
                                public void GeneratedData_Method()
                                {
                                }
                                
                                public class AutoFixtureGeneratorAttribute : DataSourceGeneratorAttribute<int, string>
                                {
                                    public override IEnumerable<(int, string)> GenerateDataSources(DataGeneratorMetadata metadata)
                                    {
                                        return [];
                                    }
                                }
                            }
                            """;
        
        var expected = Verifier.Diagnostic(Rules.WrongArgumentTypeTestDataSource)
            .WithLocation(0)
            .WithMessage("The data source type `int, string` doesn't match the parameter type `int`");

        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
    
    [Test]
    public async Task No_Property_Error()
    {
        const string text = """
                            using TUnit.Core;
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
                                    public override IEnumerable<T> GenerateDataSources(DataGeneratorMetadata metadata)
                                    {
                                        return [];
                                    }
                                }
                            }
                            """;

        await Verifier.VerifyAnalyzerAsync(text);
    }

    #endregion
}