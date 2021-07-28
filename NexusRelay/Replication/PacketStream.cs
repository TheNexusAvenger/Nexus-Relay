/*
 * TheNexusAvenger
 *
 * Wraps a stream for sending and receiving packets.
 */

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NexusRelay.Replication
{
    public enum PacketType
    {
        PingSend,
        PingResponse,
        RequestPort,
        RequestConnection,
        UdpPacketSent,
    }

    public class PacketData
    {
        /// <summary>
        /// Type of the packet.
        /// </summary>
        public PacketType Type { get; }
        
        /// <summary>
        /// Payload of the packet.
        /// </summary>
        public byte[] Payload { get; }

        /// <summary>
        /// Creates a packet.
        /// </summary>
        /// <param name="type">Type of the packet.</param>
        public PacketData(PacketType type)
        {
            this.Type = type;
            this.Payload = Array.Empty<byte>();
        }
        
        /// <summary>
        /// Creates a packet.
        /// </summary>
        /// <param name="type">Type of the packet.</param>
        /// <param name="data">Payload of the packet.</param>
        public PacketData(PacketType type, string data)
        {
            this.Type = type;
            this.Payload = Encoding.UTF8.GetBytes(data);
        }
        
        /// <summary>
        /// Creates a packet.
        /// </summary>
        /// <param name="type">Type of the packet.</param>
        /// <param name="data">Payload of the packet.</param>
        public PacketData(PacketType type, byte[] data)
        {
            this.Type = type;
            this.Payload = data;
        }
        
        /// <summary>
        /// Returns the payload as a string.
        /// </summary>
        /// <returns>The payload as a string.</returns>
        public string GetPayload()
        {
            return Encoding.UTF8.GetString(this.Payload);
        }
    }

    public class PacketStream
    {
        /// <summary>
        /// Stream to read and write data to.
        /// </summary>
        private readonly Stream _stream;
        
        /// <summary>
        /// Semaphore for the stream to prevent threads writing to the stream concurrently.
        /// </summary>
        private readonly SemaphoreSlim _streamSemaphore = new SemaphoreSlim(1, 1);
        
        /// <summary>
        /// Creates the packet stream.
        /// </summary>
        /// <param name="stream">Base stream to read data from, such as a network stream.</param>
        public PacketStream(Stream stream)
        {
            this._stream = stream;
        }
        
        /// <summary>
        /// Sends a message.
        /// </summary>
        /// <param name="message">Message packet to send.</param>
        public async Task SendAsync(PacketData message)
        {
            await this._streamSemaphore.WaitAsync();
            await this._stream.WriteAsync(BitConverter.GetBytes((uint) message.Payload.Length + 1));
            await this._stream.WriteAsync(new [] {(byte) message.Type});
            await this._stream.WriteAsync(message.Payload);
            this._streamSemaphore.Release();
        }
        
        /// <summary>
        /// Receives a message.
        /// </summary>
        /// <returns>Message that was recieved.</returns>
        public async Task<PacketData> ReceiveAsync()
        {
            // Get the packet length and throw an exception if the connection closed.
            var packetLenBuffer = new byte[4];
            var bytesRead = await this._stream.ReadAsync(packetLenBuffer);
            if (bytesRead == 0)
            {
                throw new InvalidOperationException("Connection closed.");
            }
            
            // Read and return the data.
            var packetType = new byte[1];
            await this._stream.ReadAsync(packetType);
            var packetBuffer = new byte[BitConverter.ToInt32(packetLenBuffer) - 1];
            if (packetBuffer.Length != 0)
            {
                await this._stream.ReadAsync(packetBuffer);
            }

            return new PacketData((PacketType) packetType[0], packetBuffer);
        }
    }
}