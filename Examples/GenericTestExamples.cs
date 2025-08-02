using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TUnit.Core;
using TUnit.Assertions;

namespace TUnit.Examples;

/// <summary>
/// Examples demonstrating generic test resolution for AOT scenarios
/// </summary>
public class GenericTestExamples
{
    /// <summary>
    /// Generic test class that needs explicit type instantiation for AOT
    /// </summary>
    [GenerateGenericTest(typeof(int))]
    [GenerateGenericTest(typeof(string))]
    [GenerateGenericTest(typeof(DateTime))]
    public class GenericRepositoryTests<T> where T : IComparable<T>
    {
        private readonly List<T> _items = new();
        
        [Test]
        public async Task CanAddAndRetrieveItem(T item)
        {
            // Arrange & Act
            _items.Add(item);
            
            // Assert
            await Assert.That(_items).Contains(item);
            await Assert.That(_items.Count).IsEqualTo(1);
        }
        
        [Test]
        public void CanSortItems()
        {
            // This test will be generated for int, string, and DateTime
            _items.Sort();
            
            // Verify sorting doesn't throw
            Assert.That(_items).IsNotNull();
        }
    }
    
    /// <summary>
    /// Generic method with explicit type generation
    /// </summary>
    public class GenericMethodTests
    {
        [Test]
        [GenerateGenericTest(typeof(int), typeof(string))]
        [GenerateGenericTest(typeof(double), typeof(decimal))]
        public async Task GenericSwap<T1, T2>(T1 first, T2 second)
        {
            // Simple test to verify generic method generation
            var tuple = (first, second);
            var swapped = (tuple.Item2, tuple.Item1);
            
            await Assert.That(swapped.Item1).IsEqualTo(second);
            await Assert.That(swapped.Item2).IsEqualTo(first);
        }
        
        [Test]
        [GenerateGenericTest(typeof(List<int>))]
        [GenerateGenericTest(typeof(Dictionary<string, int>))]
        public void ComplexGenericTypes<T>() where T : new()
        {
            var instance = new T();
            Assert.That(instance).IsNotNull();
        }
    }
    
    /// <summary>
    /// Example with generic constraints
    /// </summary>
    public interface IEntity
    {
        int Id { get; }
    }
    
    public class User : IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
    
    public class Product : IEntity
    {
        public int Id { get; set; }
        public decimal Price { get; set; }
    }
    
    [GenerateGenericTest(typeof(User))]
    [GenerateGenericTest(typeof(Product))]
    public class EntityServiceTests<TEntity> where TEntity : IEntity, new()
    {
        private readonly List<TEntity> _entities = new();
        
        [Test]
        public async Task CanCreateEntity()
        {
            // Arrange
            var entity = new TEntity();
            
            // Act
            _entities.Add(entity);
            
            // Assert
            await Assert.That(_entities).HasCount(1);
            await Assert.That(_entities[0]).IsNotNull();
        }
        
        [Test]
        public void EntityHasValidId()
        {
            var entity = new TEntity();
            
            // Default ID should be 0
            Assert.That(entity.Id).IsEqualTo(0);
        }
    }
    
    /// <summary>
    /// Nested generic types example
    /// </summary>
    [GenerateGenericTest(typeof(string), typeof(int))]
    [GenerateGenericTest(typeof(DateTime), typeof(bool))]
    public class NestedGenericTests<TKey, TValue> 
        where TKey : IComparable<TKey>
    {
        private readonly Dictionary<TKey, List<TValue>> _data = new();
        
        [Test]
        public async Task CanStoreNestedData(TKey key, TValue value)
        {
            // Arrange
            if (!_data.ContainsKey(key))
            {
                _data[key] = new List<TValue>();
            }
            
            // Act
            _data[key].Add(value);
            
            // Assert
            await Assert.That(_data[key]).Contains(value);
        }
    }
    
    /// <summary>
    /// Example showing that without [GenerateGenericTest], 
    /// generic tests won't work in AOT mode
    /// </summary>
    public class NonAotGenericTest<T>
    {
        [Test]
        public void ThisWontWorkInAot()
        {
            // This test class has no [GenerateGenericTest] attribute
            // So it won't be instantiated in AOT mode
            // The source generator will skip it or generate a warning
            var typeName = typeof(T).Name;
            Assert.That(typeName).IsNotNull();
        }
    }
}