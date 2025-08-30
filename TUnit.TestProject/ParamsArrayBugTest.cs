using System;
using TUnit.Core;

namespace TUnit.TestProject
{
    public class ParamsArrayBugTest
    {
        [Test]
        [Arguments("Single", typeof(string))]
        public void TestSingleTypeParam(string expected, params Type[] types)
        {
            var result = string.Join("", types.Select(t => t.Name));
            Console.WriteLine($"Expected: {expected}, Got: {result}");
            if (expected != result)
            {
                throw new Exception($"Expected {expected} but got {result}");
            }
        }
    }
}