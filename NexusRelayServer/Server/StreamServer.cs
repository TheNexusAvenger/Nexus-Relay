/*
 * TheNexusAvenger
 *
 * Server for creating network streams.
 */

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NexusRelay.Replication;

namespace NexusRelayServer.Server
{
    public class StreamServer
    {
        /// <summary>
        /// Listener for the communication stream.
        /// </summary>
        private readonly TcpListener _listener;
        
        /// <summary>
        /// Packet stream for communicating with the traffic forwarder.
        /// </summary>
        private readonly PacketStream _stream;
        
        /// <summary>
        /// Creates the server.
        /// </summary>
        /// <param name="stream">Packet stream of the source server for creating connections.</param>
        public StreamServer(PacketStream stream)
        {
            this._listener = new TcpListener(IPAddress.Any, 0);
            this._listener.Start();
            this._stream = stream;
        }
        
        /// <summary>
        /// Returns the port the server is running on.
        /// </summary>
        /// <returns>The port the server is running on.</returns>
        private int GetPort()
        {
            return ((IPEndPoint) this._listener.LocalEndpoint).Port;
        }
        
        /// <summary>
        /// Fetches a new connection for forwarding traffic.
        /// </summary>
        /// <returns>Client for sending traffic to the source.</returns>
        public async Task<TcpClient> RequestNetworkConnection(int localConnectionPort)
        {
            // Message the client the port to connect to.
            await this._stream.SendAsync(new PacketData(PacketType.RequestConnection, this.GetPort().ToString() + "," + localConnectionPort.ToString()));
            
            // Return the next accepted connection's stream.
            return await this._listener.AcceptTcpClientAsync();
        }

        /// <summary>
        /// Sends a UDP packet to the traffic forwarder.
        /// </summary>
        /// <param name="localPort">Local port the data arrived at.</param>
        /// <param name="data">Data that was sent.</param>
        public async Task SendUdpToForwarderAsync(int localPort, byte[] data)
        {
            // Create the payload.
            var portBytes = BitConverter.GetBytes(localPort);
            var payload = new byte[portBytes.Length + data.Length];
            Array.Copy(portBytes, payload, portBytes.Length);
            Array.Copy(data, 0, payload, portBytes.Length, data.Length);
            
            // Message the client the payload.
            await this._stream.SendAsync(new PacketData(PacketType.UdpPacketSent, payload));
        }
        
        /// <summary>
        /// Closes the server.
        /// </summary>
        public void Close()
        {
            this._listener.Stop();
        }
    }
}