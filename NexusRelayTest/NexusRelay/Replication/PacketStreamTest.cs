/*
 * TheNexusAvenger
 * 
 * Tests the PacketStream class.
 */

using System.IO;
using NexusRelay.Replication;
using NUnit.Framework;

namespace NexusRelayTest.NexusRelay.Replication
{
    public class PacketStreamTest
    {
        /// <summary>
        /// Tests sending and receiving messages.
        /// </summary>
        [Test]
        public void TestPackets()
        {
            // Create the stream and send test packets.
            var memoryStream = new MemoryStream(1024);
            var packetStream = new PacketStream(memoryStream);
            packetStream.SendAsync(new PacketData(PacketType.PingResponse, "Test1")).Wait();
            packetStream.SendAsync(new PacketData(PacketType.PingResponse, "Test2")).Wait();
            packetStream.SendAsync(new PacketData(PacketType.PingResponse, "Test3")).Wait();
            
            // Read the packets and assert they are correct.
            memoryStream.Position = 0;
            Assert.AreEqual(packetStream.ReceiveAsync().Result.Payload, "Test1");
            Assert.AreEqual(packetStream.ReceiveAsync().Result.Payload, "Test2");
            Assert.AreEqual(packetStream.ReceiveAsync().Result.Payload, "Test3");
        }
    }
}