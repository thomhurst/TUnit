using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.UnitTests;

public class DefaultAssertionTests
{
        [Test]
        public async Task IsDefault_ReferenceType_Default()
        {
                string? s = null;
                await TUnitAssert.That(s).IsDefault();
        }
        
        [Test]
        public async Task IsDefault_ReferenceType_NotDefault()
        {
              await TUnitAssert.That(async () => 
              {
                      string? s = "1";
                      await TUnitAssert.That(s).IsDefault();
              }).Throws<TUnitAssertionException>();
        }
        
        [Test]
        public async Task IsDefault_ValueType_Integer_Default()
        {
                int x = 0;
                await TUnitAssert.That(x).IsDefault();
        }
        
        [Test]
        public async Task IsDefault_ValueType_Integer_NotDefault()
        {
              await TUnitAssert.That(async () =>
              {
                        int x = 1;
                        await TUnitAssert.That(x).IsDefault();
              }).Throws<TUnitAssertionException>();
        }
        
        [Test]
        public async Task IsDefault_ValueType_DateTime_Default()
        {
                DateTime dt = default;
                await TUnitAssert.That(dt).IsDefault();
        }
        
        [Test]
        public async Task IsDefault_ValueType_DateTime_NotDefault()
        {
                await TUnitAssert.That(async () =>
                {
                        var dt = DateTime.Now;
                        await TUnitAssert.That(dt).IsDefault();
                }).Throws<TUnitAssertionException>();
        }
        
        [Test]
        public async Task IsNotDefault_ReferenceType_Default()
        {
                await TUnitAssert.That(async () =>
                {
                        string? s = null;
                        await TUnitAssert.That(s).IsNotDefault();
                }).Throws<TUnitAssertionException>();
        }
        
        [Test]
        public async Task IsNotDefault_ReferenceType_NotDefault()
        {
                string? s = "1";
                await TUnitAssert.That(s).IsNotDefault();
        }
        
        [Test]
        public async Task IsNotDefault_ValueType_Integer_Default()
        {
                await TUnitAssert.That(async () =>
                {
                        int x = 0;
                        await TUnitAssert.That(x).IsNotDefault();
                }).Throws<TUnitAssertionException>();
        }
        
        [Test]
        public async Task IsNotDefault_ValueType_Integer_NotDefault()
        {
                int x = 1;
                await TUnitAssert.That(x).IsNotDefault();
        }
        
        [Test]
        public async Task IsNotDefault_ValueType_DateTime_Default()
        {
                await TUnitAssert.That(async () =>
                {
                        DateTime dt = default;
                        await TUnitAssert.That(dt).IsNotDefault();
                }).Throws<TUnitAssertionException>();
        }
        
        [Test]
        public async Task IsNotDefault_ValueType_DateTime_NotDefault()
        {
                var dt = DateTime.Now;
                await TUnitAssert.That(dt).IsNotDefault();
        }
}