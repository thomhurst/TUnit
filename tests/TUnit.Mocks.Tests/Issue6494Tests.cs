using TUnit.Mocks;

// Regression: https://github.com/thomhurst/TUnit/issues/6494
// The setup/verify extensions used to be emitted beside the mocked type. An extension member is
// only visible when its namespace is imported, so mocking a type from a namespace the test hadn't
// `using`'d compiled the Mock() call but hid every member setup behind CS1061 — which reads as
// "this member isn't supported". Reported as a name collision between two ICluster interfaces;
// the real cause reproduces with a single mock, and is fixed by emitting the surface into the
// globally-imported TUnit.Mocks.Generated namespace.
//
// These namespaces are deliberately NOT imported by the test class below.

namespace TUnit.Mocks.Tests.Issue6494.Cassandra
{
    public interface ISession
    {
        int Id { get; }
    }

    public interface ICluster
    {
        Task<ISession> ConnectAsync();

        event System.Action Connected;
    }
}

namespace TUnit.Mocks.Tests.Issue6494.Couchbase
{
    public interface ICluster
    {
        string BucketName { get; }

        Task<int> PingAsync();
    }
}

namespace TUnit.Mocks.Tests
{
    public class Issue6494Tests
    {
        [Test]
        public async Task Member_Setup_Works_Without_Importing_The_Mocked_Types_Namespace()
        {
            var cluster = Issue6494.Couchbase.ICluster.Mock();
            cluster.BucketName.Returns("bucket");

            await Assert.That(cluster.Object.BucketName).IsEqualTo("bucket");
        }

        [Test]
        public async Task Same_Short_Name_Interfaces_From_Different_Namespaces_Both_Keep_Their_Members()
        {
            var couchbase = Issue6494.Couchbase.ICluster.Mock();
            couchbase.PingAsync().Returns(1);

            var session = Issue6494.Cassandra.ISession.Mock();
            var cassandra = Issue6494.Cassandra.ICluster.Mock();
            cassandra.ConnectAsync().Returns(session.Object);

            await Assert.That(await couchbase.Object.PingAsync()).IsEqualTo(1);
            await Assert.That(await cassandra.Object.ConnectAsync()).IsSameReferenceAs(session.Object);
        }

        [Test]
        public async Task Verification_Works_Without_Importing_The_Mocked_Types_Namespace()
        {
            var cluster = Issue6494.Couchbase.ICluster.Mock();
            cluster.PingAsync().Returns(1);

            _ = await cluster.Object.PingAsync();

            cluster.PingAsync().WasCalled(Times.Once);
        }

        [Test]
        public async Task Events_Surface_Works_Without_Importing_The_Mocked_Types_Namespace()
        {
            var cluster = Issue6494.Cassandra.ICluster.Mock();
            var fired = 0;
            cluster.Object.Connected += () => fired++;

            cluster.RaiseConnected();

            await Assert.That(fired).IsEqualTo(1);
        }
    }
}
