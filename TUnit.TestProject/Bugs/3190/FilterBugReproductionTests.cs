using System;
using System.Threading.Tasks;
using TUnit.Core;

namespace TUnit.TestProject.Bugs._3190
{
    // Test classes to reproduce the issue from #3190
    // This reproduces the exact scenario described in the bug report
    
    public class TestClass1WithoutExplicit
    {
        [Test]
        [Category("Performance")]
        public async Task TestClass1TestMethod1()
        {
            Console.WriteLine("TestClass1TestMethod1");
        }

        [Test]
        [Property("CI", "false")]
        public async Task TestClass1TestMethod2()
        {
            Console.WriteLine("TestClass1TestMethod2");
        }
    }

    public class TestClass2WithExplicit
    {
        [Test]
        [Category("Performance")]
        [Property("CI", "false")]
        public async Task TestClass2TestMethod1()
        {
            Console.WriteLine("TestClass2TestMethod1");
        }

        [Test]
        [Explicit] // This causes the issue according to the bug report
        public async Task TestClass2TestMethod2()
        {
            Console.WriteLine("TestClass2TestMethod2");
        }
    }
}