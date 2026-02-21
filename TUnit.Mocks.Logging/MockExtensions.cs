namespace TUnit.Mocks;

/// <summary>
/// Extends <see cref="Mock"/> with logger factory methods.
/// Available when the <c>TUnit.Mocks.Logging</c> package is referenced.
/// </summary>
public static class MockExtensions
{
    extension(Mock)
    {
        /// <summary>
        /// Creates a new <see cref="Logging.MockLogger"/>.
        /// </summary>
        public static Logging.MockLogger Logger() => new();

        /// <summary>
        /// Creates a new <see cref="Logging.MockLogger"/> with a category name.
        /// </summary>
        public static Logging.MockLogger Logger(string categoryName) => new(categoryName);

        /// <summary>
        /// Creates a new <see cref="Logging.MockLogger{TCategoryName}"/> for the specified type.
        /// </summary>
        public static Logging.MockLogger<T> Logger<T>() => new();
    }
}
