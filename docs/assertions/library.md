# Assertions Library

Browse the full list of assertions available in TUnit. Use the search box to filter by name or category. Click any assertion to see its signature and usage details.

# Assertions Library

Search and explore all available TUnit assertions

Search assertions...

AllAsyncBooleanCollectionsComparisonDateTimeDefaultDictionaryEqualityExceptionsNullNumericStringType

52<!-- --> assertion<!-- -->s<!-- --> found

### IsEqualTo

Equality

Asserts the value equals the expected value

`await Assert.That(actual).IsEqualTo(expected)`

Example:`await Assert.That(5).IsEqualTo(5)`

### IsNotEqualTo

Equality

Asserts the value does not equal the expected value

`await Assert.That(actual).IsNotEqualTo(expected)`

Example:`await Assert.That(5).IsNotEqualTo(3)`

### IsGreaterThan

Comparison

Asserts the value is greater than expected

`await Assert.That(actual).IsGreaterThan(expected)`

Example:`await Assert.That(10).IsGreaterThan(5)`

### IsLessThan

Comparison

Asserts the value is less than expected

`await Assert.That(actual).IsLessThan(expected)`

Example:`await Assert.That(3).IsLessThan(10)`

### IsGreaterThanOrEqualTo

Comparison

Asserts the value is greater than or equal to expected

`await Assert.That(actual).IsGreaterThanOrEqualTo(expected)`

Example:`await Assert.That(5).IsGreaterThanOrEqualTo(5)`

### IsLessThanOrEqualTo

Comparison

Asserts the value is less than or equal to expected

`await Assert.That(actual).IsLessThanOrEqualTo(expected)`

Example:`await Assert.That(5).IsLessThanOrEqualTo(10)`

### IsNull

Null

Asserts the value is null

`await Assert.That(actual).IsNull()`

Example:`await Assert.That(nullValue).IsNull()`

### IsNotNull

Null

Asserts the value is not null

`await Assert.That(actual).IsNotNull()`

Example:`await Assert.That(value).IsNotNull()`

### IsDefault

Default

Asserts the value is the default value for its type

`await Assert.That(actual).IsDefault()`

Example:`await Assert.That(0).IsDefault()`

### IsNotDefault

Default

Asserts the value is not the default value

`await Assert.That(actual).IsNotDefault()`

Example:`await Assert.That(5).IsNotDefault()`

### IsTrue

Boolean

Asserts the value is true

`await Assert.That(actual).IsTrue()`

Example:`await Assert.That(condition).IsTrue()`

### IsFalse

Boolean

Asserts the value is false

`await Assert.That(actual).IsFalse()`

Example:`await Assert.That(!condition).IsFalse()`

### Contains

String

Asserts the string contains the expected substring

`await Assert.That(actual).Contains(substring)`

Example:`await Assert.That("hello world").Contains("world")`

### DoesNotContain

String

Asserts the string does not contain the substring

`await Assert.That(actual).DoesNotContain(substring)`

Example:`await Assert.That("hello").DoesNotContain("world")`

### StartsWith

String

Asserts the string starts with the expected prefix

`await Assert.That(actual).StartsWith(prefix)`

Example:`await Assert.That("hello world").StartsWith("hello")`

### EndsWith

String

Asserts the string ends with the expected suffix

`await Assert.That(actual).EndsWith(suffix)`

Example:`await Assert.That("hello world").EndsWith("world")`

### IsEmpty

String

Asserts the string is empty

`await Assert.That(actual).IsEmpty()`

Example:`await Assert.That("").IsEmpty()`

### IsNotEmpty

String

Asserts the string is not empty

`await Assert.That(actual).IsNotEmpty()`

Example:`await Assert.That("text").IsNotEmpty()`

### Length

String

Asserts the string has the expected length

`await Assert.That(actual).Length().IsEqualTo(length)`

Example:`await Assert.That("hello").Length().IsEqualTo(5)`

### Contains (Collection)

Collections

Asserts the collection contains the expected item

`await Assert.That(collection).Contains(item)`

Example:`await Assert.That(list).Contains(5)`

### DoesNotContain (Collection)

Collections

Asserts the collection does not contain the item

`await Assert.That(collection).DoesNotContain(item)`

Example:`await Assert.That(list).DoesNotContain(10)`

### Count

Collections

Asserts the collection has the expected count

`await Assert.That(collection).Count().IsEqualTo(count)`

Example:`await Assert.That(list).Count().IsEqualTo(3)`

### IsEmpty (Collection)

Collections

Asserts the collection is empty

`await Assert.That(collection).IsEmpty()`

Example:`await Assert.That(emptyList).IsEmpty()`

### IsNotEmpty (Collection)

Collections

Asserts the collection is not empty

`await Assert.That(collection).IsNotEmpty()`

Example:`await Assert.That(list).IsNotEmpty()`

### IsEquivalentTo

Collections

Asserts collections have equivalent items (order-independent)

`await Assert.That(collection).IsEquivalentTo(expected)`

Example:`await Assert.That(list).IsEquivalentTo([1,2,3])`

### HasSingleItem

Collections

Asserts the collection has exactly one item

`await Assert.That(collection).HasSingleItem()`

Example:`await Assert.That(singleItemList).HasSingleItem()`

### IsPositive

Numeric

Asserts the number is positive

`await Assert.That(actual).IsPositive()`

Example:`await Assert.That(5).IsPositive()`

### IsNegative

Numeric

Asserts the number is negative

`await Assert.That(actual).IsNegative()`

Example:`await Assert.That(-5).IsNegative()`

### IsZero

Numeric

Asserts the number is zero

`await Assert.That(actual).IsZero()`

Example:`await Assert.That(0).IsZero()`

### IsNotZero

Numeric

Asserts the number is not zero

`await Assert.That(actual).IsNotZero()`

Example:`await Assert.That(5).IsNotZero()`

### IsEven

Numeric

Asserts the number is even

`await Assert.That(actual).IsEven()`

Example:`await Assert.That(4).IsEven()`

### IsOdd

Numeric

Asserts the number is odd

`await Assert.That(actual).IsOdd()`

Example:`await Assert.That(3).IsOdd()`

### IsBetween

Numeric

Asserts the number is between min and max

`await Assert.That(actual).IsBetween(min, max)`

Example:`await Assert.That(5).IsBetween(1, 10)`

### Throws

Exceptions

Asserts the action throws an exception

`await Assert.That(() => action()).Throws()`

Example:`await Assert.That(() => throw new Exception()).Throws()`

### ThrowsExactly

Exceptions

Asserts the action throws exactly the specified exception type

`await Assert.That(() => action()).ThrowsExactly<TException>()`

Example:`await Assert.That(() => throw new ArgumentException()).ThrowsExactly<ArgumentException>()`

### ThrowsException

Exceptions

Asserts the action throws the specified exception type or derived

`await Assert.That(() => action()).ThrowsException<TException>()`

Example:`await Assert.That(() => throw new InvalidOperationException()).ThrowsException<Exception>()`

### ThrowsWithMessage

Exceptions

Asserts the exception has the expected message

`await Assert.That(() => action()).Throws().WithMessage(message)`

Example:`await Assert.That(() => throw new Exception("error")).Throws().WithMessage("error")`

### IsTypeOf

Type

Asserts the value is exactly the specified type

`await Assert.That(actual).IsTypeOf<TType>()`

Example:`await Assert.That(obj).IsTypeOf<MyClass>()`

### IsAssignableTo

Type

Asserts the value is assignable to the specified type

`await Assert.That(actual).IsAssignableTo<TType>()`

Example:`await Assert.That(derived).IsAssignableTo<BaseClass>()`

### IsAssignableFrom

Type

Asserts the type is assignable from the value

`await Assert.That(actual).IsAssignableFrom<TType>()`

Example:`await Assert.That(baseInstance).IsAssignableFrom<DerivedClass>()`

### IsNotAssignableFrom

Type

Asserts the type is not assignable from the value

`await Assert.That(actual).IsNotAssignableFrom<TType>()`

Example:`await Assert.That(dog).IsNotAssignableFrom<string>()`

### IsAfter

DateTime

Asserts the date is after the expected date

`await Assert.That(actual).IsAfter(expected)`

Example:`await Assert.That(DateTime.Now).IsAfter(yesterday)`

### IsBefore

DateTime

Asserts the date is before the expected date

`await Assert.That(actual).IsBefore(expected)`

Example:`await Assert.That(yesterday).IsBefore(DateTime.Now)`

### IsOnOrAfter

DateTime

Asserts the date is on or after the expected date

`await Assert.That(actual).IsOnOrAfter(expected)`

Example:`await Assert.That(DateTime.Now).IsOnOrAfter(today)`

### IsOnOrBefore

DateTime

Asserts the date is on or before the expected date

`await Assert.That(actual).IsOnOrBefore(expected)`

Example:`await Assert.That(today).IsOnOrBefore(DateTime.Now)`

### ContainsKey

Dictionary

Asserts the dictionary contains the specified key

`await Assert.That(dict).ContainsKey(key)`

Example:`await Assert.That(dict).ContainsKey("key")`

### DoesNotContainKey

Dictionary

Asserts the dictionary does not contain the key

`await Assert.That(dict).DoesNotContainKey(key)`

Example:`await Assert.That(dict).DoesNotContainKey("missing")`

### ContainsValue

Dictionary

Asserts the dictionary contains the specified value

`await Assert.That(dict).ContainsValue(value)`

Example:`await Assert.That(dict).ContainsValue("value")`

### IsCompleted

Async

Asserts the task is completed

`await Assert.That(task).IsCompleted()`

Example:`await Assert.That(Task.CompletedTask).IsCompleted()`

### IsNotCompleted

Async

Asserts the task is not completed

`await Assert.That(task).IsNotCompleted()`

Example:`await Assert.That(runningTask).IsNotCompleted()`

### IsFaulted

Async

Asserts the task is faulted

`await Assert.That(task).IsFaulted()`

Example:`await Assert.That(faultedTask).IsFaulted()`

### IsCancelled

Async

Asserts the task is cancelled

`await Assert.That(task).IsCancelled()`

Example:`await Assert.That(cancelledTask).IsCancelled()`
