/*
 * TheNexusAvenger
 *
 * Tests the StreamReplicator class.
 */

using System.IO;
using System.Text;
using System.Threading;
using NexusRelay.Replication;
using NUnit.Framework;

namespace NexusRelayTest.NexusRelay.Replication
{
    public class StreamReplicatorTest
    {
        /// <summary>
        /// Tests replicating memory streams.
        /// </summary>
        [Test]
        public void TestReplicateMemoryStream()
        {
            // Create the streams.
            var sourceStream = new MemoryStream(4);
            var targetStream = new MemoryStream(4);
            sourceStream.Write(Encoding.ASCII.GetBytes("Test"));
            sourceStream.Position = 0;

            // Replicate the stream.
            var replicator = new StreamReplicator(sourceStream, targetStream);
            replicator.ReplicateStream(new CancellationToken()).Wait();
            
            // Assert the stream was replicated.
            targetStream.Position = 0;
            var bytes = new byte[4];
            targetStream.Read(bytes);
            Assert.AreEqual(Encoding.ASCII.GetString(bytes), "Test", "Replicated data is incorrect.");
        }
    }
}