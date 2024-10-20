#pragma warning disable CS9113 // Parameter is unread.
namespace TUnit.TestProject;

    [Arguments("1")]
    [Arguments("2")]
    public class ClassAndMethodArgumentsTests(string arg1)
    {
        [Test]
        public Task Simple() => Task.CompletedTask;

        [Test]
        [Arguments("3")]
        [Arguments("4")]
        public Task WithMethodLevel(string arg2) => Task.CompletedTask;

        [Test]
        [Arguments("3")]
        [Arguments("4")]
        public Task IgnoreParameters(string arg2) => Task.CompletedTask;
    }