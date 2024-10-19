namespace TUnit.Assertions.Exceptions;

public class MixedAndOrAssertionsException()
    : AssertionException("Don't mix 'Or' & 'And' operators in assertions (Consider using Assertion Groups as an alternative).");