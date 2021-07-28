/*
 * TheNexusAvenger
 *
 * Instance for forwarding traffic from a port.
 */

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;
using NexusRelay;
using NexusRelay.Replication;

namespace NexusRelayServer.Server
{
    public class HostInstance
    {
        /// <summary>
        /// Interval to send ping requests.
        /// </summary>
        private const int PingIntervalSeconds = 5;
        
        /// <summary>
        /// Local port for accepting traffic.
        /// </summary>
        private readonly int _localPort;
        
        /// <summary>
        /// Client for accepting connections.
        /// </summary>        
        private TcpClient _client;
        
        /// <summary>
        /// Packet stream for communicating with the traffic forwarder.
        /// </summary>
        private readonly PacketStream _clientCommunicationStream;
        
        /// <summary>
        /// Server for creating streams.
        /// </summary>
        private StreamServer _streamServer;
        
        /// <summary>
        /// Server for sending traffic.
        /// </summary>
        private TrafficServer _trafficServer;
        
        /// <summary>
        /// Creates a host instance.
        /// </summary>
        /// <param name="localPort">Local port to accept traffic on.</param>
        /// <param name="client">Client for communicating with the traffic forwarder source.</param>
        public HostInstance(int localPort, TcpClient client)
        {
            this._localPort = localPort;
            this._client = client;
            this._clientCommunicationStream = new PacketStream(client.GetStream());
        }
        
        /// <summary>
        /// Starts hosting the server.
        /// </summary>
        public async Task StartAsync()
        {
            // Start accepting packet messages.
            var lastPingResponse = new Stopwatch();
            lastPingResponse.Start();
            var _ = Task.Run(async () =>
            {
                while (this._client != null)
                {
                    try
                    {
                        // Receive a packet.
                        var packetData = await this._clientCommunicationStream.ReceiveAsync();
                        switch (packetData.Type)
                        {
                            case PacketType.PingResponse:
                                // Store the last ping time.
                                lastPingResponse.Reset();
                                break;
                            case PacketType.UdpPacketSent:
                                // Read the data.
                                var serverLocalPortBytes = new byte[4];
                                var udpPacketBytes = new byte[packetData.Payload.Length - 4];
                                Array.Copy(packetData.Payload, serverLocalPortBytes, 4);
                                Array.Copy(packetData.Payload, 4, udpPacketBytes, 0, udpPacketBytes.Length);
                                var serverLocalPort = BinaryPrimitives.ReadInt32LittleEndian(serverLocalPortBytes);
                            
                                // Forward the UDP packet.
                                var ___ = Task.Run(async () =>
                                {
                                    await this._trafficServer.SendUdpAsyncToClientAsync(serverLocalPort, udpPacketBytes);
                                });
                                break;
                            default:
                                throw new InvalidOperationException($"Invalid type: {packetData.Type}");
                        }
                    }
                    catch (Exception e)
                    {
                        // Close the connection if an exception occured (client disconnected).
                        Logger.Error($"Exception occured getting ping response; closing.\n\t{e}");
                        this.Close();
                        return;
                    }
                }
            });
            
            // Start the pings.
            var __ = Task.Run(async () =>
            {
                while (this._client != null)
                {
                    // Wait to send a ping.
                    await Task.Delay(PingIntervalSeconds * 1000);
                    
                    // Stop if 3 ping requests were sent with no responses.
                    if (lastPingResponse.ElapsedMilliseconds > (3 * PingIntervalSeconds * 1000))
                    {
                        Logger.Warning("No ping response sent; closing.");
                        this.Close();
                    }
                    
                    try
                    {
                        // Send the ping request.
                        this._clientCommunicationStream.SendAsync(new PacketData(PacketType.PingSend)).Wait();
                    }
                    catch (Exception e)
                    {
                        // Close the connection if an exception occured (client disconnected).
                        Logger.Error($"Exception occured getting ping response; closing.\n\t{e}");
                        this.Close();
                        return;
                    }
                }
            });
            
            // Create the stream creator server and TCP listener server.
            this._streamServer = new StreamServer(this._clientCommunicationStream);
            this._trafficServer = new TrafficServer(this._localPort, this._streamServer);
            await this._trafficServer.StartAsync();
            
            // Close the server.
            this.Close();
        }
        
        /// <summary>
        /// Closes the server.
        /// </summary>
        public void Close()
        {
            if (this._client == null) return;
            this._client.Close();
            this._client = null;
            this._streamServer.Close();
            this._trafficServer.Close();
        }
    }
}