/*
 * TheNexusAvenger
 *
 * Server for accepting traffic.
 */

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NexusRelay.Extension;
using NexusRelay.Replication;

namespace NexusRelayServer.Server
{
    public class TrafficServer
    {
        /// <summary>
        /// TCP listener for accepting traffic.
        /// </summary>
        private readonly TcpListener _tcpListener;
        
        /// <summary>
        /// UDP listener for accepting traffic.
        /// </summary>
        private readonly UdpClient _udpListener;
        
        /// <summary>
        /// Stream server for opening connections.
        /// </summary>
        private readonly StreamServer _streamServer;
        
        /// <summary>
        /// Cancellation tokens for the connections.
        /// </summary>
        private List<CancellationTokenSource> _connectionCancellationTokenSources = new List<CancellationTokenSource>();
        
        /// <summary>
        /// IP endpoints that are connected.
        /// </summary>
        private readonly Dictionary<int, IPEndPoint> _knownConnectionLocalPorts = new Dictionary<int, IPEndPoint>();
        
        /// <summary>
        /// Creates the server.
        /// </summary>
        /// <param name="port">Port to accept connections.</param>
        /// <param name="streamServer">Stream server for creating connections to the source client.</param>
        public TrafficServer(int port, StreamServer streamServer)
        {
            this._tcpListener = new TcpListener(IPAddress.Any, port);
            this._udpListener = new UdpClient(port);
            this._streamServer = streamServer;
        }

        /// <summary>
        /// Starts accepting connections.
        /// </summary>
        public async Task StartAsync()
        {
            // Start the listener.
            this._tcpListener.Start();
            
            // Start accepting UDP packets.
            var _ = Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        // Receive the packet.
                        var packet = await this._udpListener.ReceiveAsync();
                        
                        // Return if the port is unknown.
                        var connectedPort = packet.RemoteEndPoint.Port;
                        if (!this._knownConnectionLocalPorts.ContainsKey(connectedPort))
                        {
                            return;
                        }
                        
                        // Send the UDP packet.
                        await this._streamServer.SendUdpToForwarderAsync(connectedPort, packet.Buffer);
                    }
                }
                catch (Exception)
                {
                    // Ignore exceptions.
                }
            });
            
            // Start accepting TCP connections.
            try
            {
                while (true)
                {
                    // Listen for a connection and get the connection to connect to.
                    var connection = await this._tcpListener.AcceptTcpClientAsync();
                    var connectedPort = connection.GetRemoteEndPoint().Port;
                    var trafficClient = await this._streamServer.RequestNetworkConnection(connectedPort);

                    // Start the connection.
                    var __ = Task.Run(async () =>
                    {
                        await this.ProcessConnectionAsync(connection, trafficClient);
                    });
                }
            }
            catch (Exception)
            {
                // Ignore exceptions.
            }
        }
        
        /// <summary>
        /// Starts processing a connection.
        /// </summary>
        /// <param name="client">Client attempting to connect.</param>
        /// <param name="trafficClient">Client to send traffic to.</param>
        private async Task ProcessConnectionAsync(TcpClient client, TcpClient trafficClient)
        {
            // Forward traffic between the client and server.
            var connectedPort = client.GetRemoteEndPoint().Port;
            this._knownConnectionLocalPorts[connectedPort] = client.GetRemoteEndPoint();
            var cancellationToken = new CancellationTokenSource();
            this._connectionCancellationTokenSources.Add(cancellationToken);
            var replicator = new BidirectionalStreamReplicator(trafficClient.GetStream(), client.GetStream());
            await replicator.ReplicateStream(cancellationToken.Token);
            
            // Close the clients.
            this._connectionCancellationTokenSources.Remove(cancellationToken);
            cancellationToken.Cancel();
            client.Close();
            trafficClient.Close();
        }

        /// <summary>
        /// Sends a UDP message to the connected client.
        /// </summary>
        /// <param name="serverLocalPort">The port the user connected to.</param>
        /// <param name="udpPacketBytes">The data of the packet.</param>
        public async Task SendUdpAsyncToClientAsync(int serverLocalPort, byte[] udpPacketBytes)
        {
            if (this._knownConnectionLocalPorts.ContainsKey(serverLocalPort))
            {
                await this._udpListener.SendAsync(udpPacketBytes, udpPacketBytes.Length, this._knownConnectionLocalPorts[serverLocalPort]);
            }
        }
        
        /// <summary>
        /// Closes the server.
        /// </summary>
        public void Close()
        {
            // Close the listeners.
            this._tcpListener.Stop();
            this._udpListener.Close();
            
            // Close the connections.
            foreach (var token in this._connectionCancellationTokenSources.ToArray())
            {
                token.Cancel();
            }
            this._connectionCancellationTokenSources = new List<CancellationTokenSource>();
        }
    }
}