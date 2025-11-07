import React, { useState, useMemo } from 'react';
import styles from './styles.module.css';

interface Assertion {
  name: string;
  category: string;
  description: string;
  syntax: string;
  example?: string;
}

const assertionsData: Assertion[] = [
  // Equality & Comparison
  { name: 'IsEqualTo', category: 'Equality', description: 'Asserts the value equals the expected value', syntax: 'await Assert.That(actual).IsEqualTo(expected)', example: 'await Assert.That(5).IsEqualTo(5)' },
  { name: 'IsNotEqualTo', category: 'Equality', description: 'Asserts the value does not equal the expected value', syntax: 'await Assert.That(actual).IsNotEqualTo(expected)', example: 'await Assert.That(5).IsNotEqualTo(3)' },
  { name: 'IsGreaterThan', category: 'Comparison', description: 'Asserts the value is greater than expected', syntax: 'await Assert.That(actual).IsGreaterThan(expected)', example: 'await Assert.That(10).IsGreaterThan(5)' },
  { name: 'IsLessThan', category: 'Comparison', description: 'Asserts the value is less than expected', syntax: 'await Assert.That(actual).IsLessThan(expected)', example: 'await Assert.That(3).IsLessThan(10)' },
  { name: 'IsGreaterThanOrEqualTo', category: 'Comparison', description: 'Asserts the value is greater than or equal to expected', syntax: 'await Assert.That(actual).IsGreaterThanOrEqualTo(expected)', example: 'await Assert.That(5).IsGreaterThanOrEqualTo(5)' },
  { name: 'IsLessThanOrEqualTo', category: 'Comparison', description: 'Asserts the value is less than or equal to expected', syntax: 'await Assert.That(actual).IsLessThanOrEqualTo(expected)', example: 'await Assert.That(5).IsLessThanOrEqualTo(10)' },

  // Null & Default
  { name: 'IsNull', category: 'Null', description: 'Asserts the value is null', syntax: 'await Assert.That(actual).IsNull()', example: 'await Assert.That(nullValue).IsNull()' },
  { name: 'IsNotNull', category: 'Null', description: 'Asserts the value is not null', syntax: 'await Assert.That(actual).IsNotNull()', example: 'await Assert.That(value).IsNotNull()' },
  { name: 'IsDefault', category: 'Default', description: 'Asserts the value is the default value for its type', syntax: 'await Assert.That(actual).IsDefault()', example: 'await Assert.That(0).IsDefault()' },
  { name: 'IsNotDefault', category: 'Default', description: 'Asserts the value is not the default value', syntax: 'await Assert.That(actual).IsNotDefault()', example: 'await Assert.That(5).IsNotDefault()' },

  // Boolean
  { name: 'IsTrue', category: 'Boolean', description: 'Asserts the value is true', syntax: 'await Assert.That(actual).IsTrue()', example: 'await Assert.That(condition).IsTrue()' },
  { name: 'IsFalse', category: 'Boolean', description: 'Asserts the value is false', syntax: 'await Assert.That(actual).IsFalse()', example: 'await Assert.That(!condition).IsFalse()' },

  // String
  { name: 'Contains', category: 'String', description: 'Asserts the string contains the expected substring', syntax: 'await Assert.That(actual).Contains(substring)', example: 'await Assert.That("hello world").Contains("world")' },
  { name: 'DoesNotContain', category: 'String', description: 'Asserts the string does not contain the substring', syntax: 'await Assert.That(actual).DoesNotContain(substring)', example: 'await Assert.That("hello").DoesNotContain("world")' },
  { name: 'StartsWith', category: 'String', description: 'Asserts the string starts with the expected prefix', syntax: 'await Assert.That(actual).StartsWith(prefix)', example: 'await Assert.That("hello world").StartsWith("hello")' },
  { name: 'EndsWith', category: 'String', description: 'Asserts the string ends with the expected suffix', syntax: 'await Assert.That(actual).EndsWith(suffix)', example: 'await Assert.That("hello world").EndsWith("world")' },
  { name: 'IsEmpty', category: 'String', description: 'Asserts the string is empty', syntax: 'await Assert.That(actual).IsEmpty()', example: 'await Assert.That("").IsEmpty()' },
  { name: 'IsNotEmpty', category: 'String', description: 'Asserts the string is not empty', syntax: 'await Assert.That(actual).IsNotEmpty()', example: 'await Assert.That("text").IsNotEmpty()' },
  { name: 'HasLength', category: 'String', description: 'Asserts the string has the expected length', syntax: 'await Assert.That(actual).HasLength().EqualTo(length)', example: 'await Assert.That("hello").HasLength().EqualTo(5)' },

  // Collections
  { name: 'Contains (Collection)', category: 'Collections', description: 'Asserts the collection contains the expected item', syntax: 'await Assert.That(collection).Contains(item)', example: 'await Assert.That(list).Contains(5)' },
  { name: 'DoesNotContain (Collection)', category: 'Collections', description: 'Asserts the collection does not contain the item', syntax: 'await Assert.That(collection).DoesNotContain(item)', example: 'await Assert.That(list).DoesNotContain(10)' },
  { name: 'HasCount', category: 'Collections', description: 'Asserts the collection has the expected count', syntax: 'await Assert.That(collection).HasCount().EqualTo(count)', example: 'await Assert.That(list).HasCount().EqualTo(3)' },
  { name: 'IsEmpty (Collection)', category: 'Collections', description: 'Asserts the collection is empty', syntax: 'await Assert.That(collection).IsEmpty()', example: 'await Assert.That(emptyList).IsEmpty()' },
  { name: 'IsNotEmpty (Collection)', category: 'Collections', description: 'Asserts the collection is not empty', syntax: 'await Assert.That(collection).IsNotEmpty()', example: 'await Assert.That(list).IsNotEmpty()' },
  { name: 'IsEquivalentTo', category: 'Collections', description: 'Asserts collections have equivalent items (order-independent)', syntax: 'await Assert.That(collection).IsEquivalentTo(expected)', example: 'await Assert.That(list).IsEquivalentTo([1,2,3])' },
  { name: 'HasSingleItem', category: 'Collections', description: 'Asserts the collection has exactly one item', syntax: 'await Assert.That(collection).HasSingleItem()', example: 'await Assert.That(singleItemList).HasSingleItem()' },

  // Numeric
  { name: 'IsPositive', category: 'Numeric', description: 'Asserts the number is positive', syntax: 'await Assert.That(actual).IsPositive()', example: 'await Assert.That(5).IsPositive()' },
  { name: 'IsNegative', category: 'Numeric', description: 'Asserts the number is negative', syntax: 'await Assert.That(actual).IsNegative()', example: 'await Assert.That(-5).IsNegative()' },
  { name: 'IsZero', category: 'Numeric', description: 'Asserts the number is zero', syntax: 'await Assert.That(actual).IsZero()', example: 'await Assert.That(0).IsZero()' },
  { name: 'IsNotZero', category: 'Numeric', description: 'Asserts the number is not zero', syntax: 'await Assert.That(actual).IsNotZero()', example: 'await Assert.That(5).IsNotZero()' },
  { name: 'IsEven', category: 'Numeric', description: 'Asserts the number is even', syntax: 'await Assert.That(actual).IsEven()', example: 'await Assert.That(4).IsEven()' },
  { name: 'IsOdd', category: 'Numeric', description: 'Asserts the number is odd', syntax: 'await Assert.That(actual).IsOdd()', example: 'await Assert.That(3).IsOdd()' },
  { name: 'IsBetween', category: 'Numeric', description: 'Asserts the number is between min and max', syntax: 'await Assert.That(actual).IsBetween(min, max)', example: 'await Assert.That(5).IsBetween(1, 10)' },

  // Exceptions
  { name: 'Throws', category: 'Exceptions', description: 'Asserts the action throws an exception', syntax: 'await Assert.That(() => action()).Throws()', example: 'await Assert.That(() => throw new Exception()).Throws()' },
  { name: 'ThrowsExactly', category: 'Exceptions', description: 'Asserts the action throws exactly the specified exception type', syntax: 'await Assert.That(() => action()).ThrowsExactly<TException>()', example: 'await Assert.That(() => throw new ArgumentException()).ThrowsExactly<ArgumentException>()' },
  { name: 'ThrowsException', category: 'Exceptions', description: 'Asserts the action throws the specified exception type or derived', syntax: 'await Assert.That(() => action()).ThrowsException<TException>()', example: 'await Assert.That(() => throw new InvalidOperationException()).ThrowsException<Exception>()' },
  { name: 'ThrowsWithMessage', category: 'Exceptions', description: 'Asserts the exception has the expected message', syntax: 'await Assert.That(() => action()).Throws().WithMessage(message)', example: 'await Assert.That(() => throw new Exception("error")).Throws().WithMessage("error")' },

  // Type
  { name: 'IsTypeOf', category: 'Type', description: 'Asserts the value is exactly the specified type', syntax: 'await Assert.That(actual).IsTypeOf<TType>()', example: 'await Assert.That(obj).IsTypeOf<MyClass>()' },
  { name: 'IsAssignableTo', category: 'Type', description: 'Asserts the value is assignable to the specified type', syntax: 'await Assert.That(actual).IsAssignableTo<TType>()', example: 'await Assert.That(derived).IsAssignableTo<BaseClass>()' },
  { name: 'IsAssignableFrom', category: 'Type', description: 'Asserts the type is assignable from the value', syntax: 'await Assert.That(actual).IsAssignableFrom<TType>()', example: 'await Assert.That(baseInstance).IsAssignableFrom<DerivedClass>()' },

  // DateTime
  { name: 'IsAfter', category: 'DateTime', description: 'Asserts the date is after the expected date', syntax: 'await Assert.That(actual).IsAfter(expected)', example: 'await Assert.That(DateTime.Now).IsAfter(yesterday)' },
  { name: 'IsBefore', category: 'DateTime', description: 'Asserts the date is before the expected date', syntax: 'await Assert.That(actual).IsBefore(expected)', example: 'await Assert.That(yesterday).IsBefore(DateTime.Now)' },
  { name: 'IsOnOrAfter', category: 'DateTime', description: 'Asserts the date is on or after the expected date', syntax: 'await Assert.That(actual).IsOnOrAfter(expected)', example: 'await Assert.That(DateTime.Now).IsOnOrAfter(today)' },
  { name: 'IsOnOrBefore', category: 'DateTime', description: 'Asserts the date is on or before the expected date', syntax: 'await Assert.That(actual).IsOnOrBefore(expected)', example: 'await Assert.That(today).IsOnOrBefore(DateTime.Now)' },

  // Dictionary
  { name: 'ContainsKey', category: 'Dictionary', description: 'Asserts the dictionary contains the specified key', syntax: 'await Assert.That(dict).ContainsKey(key)', example: 'await Assert.That(dict).ContainsKey("key")' },
  { name: 'DoesNotContainKey', category: 'Dictionary', description: 'Asserts the dictionary does not contain the key', syntax: 'await Assert.That(dict).DoesNotContainKey(key)', example: 'await Assert.That(dict).DoesNotContainKey("missing")' },
  { name: 'ContainsValue', category: 'Dictionary', description: 'Asserts the dictionary contains the specified value', syntax: 'await Assert.That(dict).ContainsValue(value)', example: 'await Assert.That(dict).ContainsValue("value")' },

  // Tasks & Async
  { name: 'IsCompleted', category: 'Async', description: 'Asserts the task is completed', syntax: 'await Assert.That(task).IsCompleted()', example: 'await Assert.That(Task.CompletedTask).IsCompleted()' },
  { name: 'IsNotCompleted', category: 'Async', description: 'Asserts the task is not completed', syntax: 'await Assert.That(task).IsNotCompleted()', example: 'await Assert.That(runningTask).IsNotCompleted()' },
  { name: 'IsFaulted', category: 'Async', description: 'Asserts the task is faulted', syntax: 'await Assert.That(task).IsFaulted()', example: 'await Assert.That(faultedTask).IsFaulted()' },
  { name: 'IsCancelled', category: 'Async', description: 'Asserts the task is cancelled', syntax: 'await Assert.That(task).IsCancelled()', example: 'await Assert.That(cancelledTask).IsCancelled()' },
];

export default function AssertionsLibrary(): JSX.Element {
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('All');

  const categories = useMemo(() => {
    const cats = ['All', ...new Set(assertionsData.map(a => a.category))];
    return cats.sort();
  }, []);

  const filteredAssertions = useMemo(() => {
    return assertionsData.filter(assertion => {
      const matchesSearch =
        assertion.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
        assertion.description.toLowerCase().includes(searchTerm.toLowerCase()) ||
        assertion.syntax.toLowerCase().includes(searchTerm.toLowerCase());

      const matchesCategory = selectedCategory === 'All' || assertion.category === selectedCategory;

      return matchesSearch && matchesCategory;
    });
  }, [searchTerm, selectedCategory]);

  return (
    <div className={styles.assertionsLibrary}>
      <div className={styles.header}>
        <h1>Assertions Library</h1>
        <p>Search and explore all available TUnit assertions</p>
      </div>

      <div className={styles.controls}>
        <div className={styles.searchBox}>
          <input
            type="text"
            placeholder="Search assertions..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className={styles.searchInput}
          />
        </div>

        <div className={styles.categoryFilter}>
          {categories.map((category) => (
            <button
              key={category}
              className={`${styles.categoryButton} ${selectedCategory === category ? styles.active : ''}`}
              onClick={() => setSelectedCategory(category)}
            >
              {category}
            </button>
          ))}
        </div>
      </div>

      <div className={styles.results}>
        <p className={styles.resultCount}>
          {filteredAssertions.length} assertion{filteredAssertions.length !== 1 ? 's' : ''} found
        </p>

        <div className={styles.assertionsList}>
          {filteredAssertions.map((assertion, idx) => (
            <div key={idx} className={styles.assertionCard}>
              <div className={styles.assertionHeader}>
                <h3 className={styles.assertionName}>{assertion.name}</h3>
                <span className={styles.assertionCategory}>{assertion.category}</span>
              </div>
              <p className={styles.assertionDescription}>{assertion.description}</p>
              <div className={styles.assertionSyntax}>
                <code>{assertion.syntax}</code>
              </div>
              {assertion.example && (
                <div className={styles.assertionExample}>
                  <span className={styles.exampleLabel}>Example:</span>
                  <code>{assertion.example}</code>
                </div>
              )}
            </div>
          ))}
        </div>

        {filteredAssertions.length === 0 && (
          <div className={styles.noResults}>
            <p>No assertions found matching your criteria.</p>
            <p>Try adjusting your search or category filter.</p>
          </div>
        )}
      </div>
    </div>
  );
}
