/*
 * TheNexusAvenger
 *
 * Client for Nexus Relay.
 */

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NexusRelay;
using NexusRelay.Extension;
using NexusRelay.Replication;
using Timer = System.Timers.Timer;

namespace NexusRelayClient
{
    public class Client
    {
        /// <summary>
        /// Expected interval for ping requests.
        /// </summary>
        private const int PingIntervalSeconds = 5;

        /// <summary>
        /// Total ping intervals that need to not come in to restart the connection.
        /// </summary>
        private const int PingIntervalTimeoutMultiplier = 3;

        /// <summary>
        /// Attempts to reconnect before giving up.
        /// </summary>
        private const int ReconnectAttempts = 5;
        
        /// <summary>
        /// Host name of the relay host.
        /// </summary>
        private string _relayHost;
        
        /// <summary>
        /// Port of the relay communication host.
        /// </summary>
        private readonly int _relayPort;
        
        /// <summary>
        /// Port of the relay that is forwarded.
        /// </summary>
        private readonly int _relayTrafficPort;
        
        /// <summary>
        /// Host that traffic is redirected to.
        /// </summary>
        private readonly string _redirectHost;
        
        /// <summary>
        /// Port that traffic is redirected to.
        /// </summary>
        private readonly int _redirectPort;
        
        /// <summary>
        /// Client for communicating with the relay server.
        /// </summary>
        private TcpClient _client;
        
        /// <summary>
        /// Stream of packets to the relay server.
        /// </summary>
        private PacketStream _stream;
        
        /// <summary>
        /// UDP clients currently in use.
        /// </summary>
        private readonly Dictionary<int, UdpClient> _udpClients = new Dictionary<int, UdpClient>();
        
        /// <summary>
        /// Timer for resetting the connection if ping requests were not received.
        /// </summary>
        private readonly Timer _resetConnectionTimer = new Timer(PingIntervalSeconds * PingIntervalTimeoutMultiplier * 1000)
        {
            AutoReset = false,
        };
        
        /// <summary>
        /// Creates the client.
        /// </summary>
        /// <param name="relayHost">Host of the relay server.</param>
        /// <param name="relayPort">Port of the relay server.</param>
        /// <param name="relayTrafficPort">Port to host on the remote server.</param>
        /// <param name="redirectHost">Host of the traffic to forward.</param>
        /// <param name="redirectPort">Port of the traffic to forward.</param>
        public Client(string relayHost, int relayPort, int relayTrafficPort, string redirectHost, int redirectPort)
        {
            this._relayHost = relayHost;
            this._relayTrafficPort = relayTrafficPort;
            this._relayPort = relayPort;
            this._redirectHost = redirectHost;
            this._redirectPort = redirectPort;

            // Set up resetting the timer.
            this._resetConnectionTimer.Elapsed += (sender, args) =>
            {
                if (this._client == null) return;
                Logger.Warning("Ping request not sent recently. Attempting reconnection.");
                this._client.Close();
            };
        }
        
        /// <summary>
        /// Starts accepting creating connections.
        /// </summary>
        /// <param name="secret">Secret to use to authenticate.</param>
        public async Task StartConnectingAsync(string secret)
        {
            // Start the initial connections.
            await this.AcceptConnectionsAsync(secret);
            
            // Retry connecting.
            var reconnectionAttemptFailed = false;
            while (!reconnectionAttemptFailed)
            {
                // Close the client.
                Logger.Warning("Connection closed.");
                this._client.Close();
            
                // Attempt to reconnect 3 times.
                for (var i = 1; i <= ReconnectAttempts; i++)
                {
                    try
                    {
                        Logger.Info($"Attempting to reconnect (attempt {i}/{ReconnectAttempts}).");
                        await this.AcceptConnectionsAsync(secret);
                        break;
                    }
                    catch (Exception e)
                    {
                        // Ignore any exceptions from reconnecting (just try again).
                        Logger.Error(e.ToString());
                        await Task.Delay(5000);
                        
                        // Stop reconnecting if 3 attempts were reached.
                        if (i != ReconnectAttempts) continue;
                        reconnectionAttemptFailed = true;
                        Logger.Error("Reconnect failed.");
                    }
                }
            }
        }

        /// <summary>
        /// Accepts connection requests until the connection closes.
        /// </summary>
        /// <param name="secret">Secret to use to authenticate.</param>
        private async Task AcceptConnectionsAsync(string secret)
        {
            // Set up the client.
            this._client = new TcpClient(this._relayHost,this._relayPort);
            this._stream = new PacketStream(this._client.GetStream());
            this._relayHost = this._client.GetRemoteEndPoint()?.Address.ToString() ?? this._relayHost;
            
            // Send the secret.
            this._client.GetStream().Write(Encoding.ASCII.GetBytes(secret));
            
            // Send the traffic port.
            await this._stream.SendAsync(new PacketData(PacketType.RequestPort, _relayTrafficPort.ToString()));
            Logger.Info($"Forwarding traffic from {this._relayHost}:{this._relayTrafficPort} to {this._redirectHost}:{this._redirectPort}");

            // Start the reset timer.
            this._resetConnectionTimer.Stop();
            this._resetConnectionTimer.Start();
            
            // Start accepting connections.
            try
            {
                while (true)
                {
                    var packetData = await this._stream.ReceiveAsync();
                    switch (packetData.Type)
                    {
                        case PacketType.PingSend:
                            // Send the ping response.
                            Logger.Debug("Got ping request.");
                            await this._stream.SendAsync(new PacketData(PacketType.PingResponse));
                            this._resetConnectionTimer.Stop();
                            this._resetConnectionTimer.Start();
                            Logger.Debug("Sent ping response.");
                            break;
                        case PacketType.RequestConnection:
                            // Initialize a new connection on the requested port.
                            Logger.Debug("Got connection request.");
                            var _ = Task.Run(async () =>
                            {
                                var ports = packetData.GetPayload().Split(",");
                                await this.StartConnectionAsync(int.Parse(ports[0]), int.Parse(ports[1]));
                            });
                            break;
                        case PacketType.UdpPacketSent:
                            // Read the data.
                            var serverLocalPortBytes = new byte[4];
                            var udpPacketBytes = new byte[packetData.Payload.Length - 4];
                            Array.Copy(packetData.Payload, serverLocalPortBytes, 4);
                            Array.Copy(packetData.Payload, 4, udpPacketBytes, 0, udpPacketBytes.Length);
                            var serverLocalPort = BinaryPrimitives.ReadInt32LittleEndian(serverLocalPortBytes);
                            
                            // Forward the UDP packet.
                            var __ = Task.Run(async () =>
                            {
                                await this.SendUdp(serverLocalPort, udpPacketBytes);
                            });
                            break;
                        default:
                            throw new InvalidOperationException($"Invalid type: {packetData.Type}");
                    }
                }
            }
            catch (Exception)
            {
                // Ignore exceptions.
            }
        }
        
        /// <summary>
        /// Initializes a new connection.
        /// </summary>
        /// <param name="targetPort">Port on the remote host to connect to.</param>
        /// <param name="serverLocalPort">Port on the remote host that the client connected to.</param>
        private async Task StartConnectionAsync(int targetPort, int serverLocalPort)
        {
            // Create the connections.
            var trafficClient = new TcpClient(this._relayHost, targetPort);
            var redirectClient = new TcpClient(this._redirectHost, this._redirectPort);
            var redirectClientPort = redirectClient.GetLocalEndPoint().Port;
            this._udpClients[serverLocalPort] = new UdpClient(redirectClientPort);
            
            // Forward traffic between the client and server.
            var cancellationToken = new CancellationTokenSource();
            var replicator = new BidirectionalStreamReplicator(trafficClient.GetStream(), redirectClient.GetStream());
            await replicator.ReplicateStream(cancellationToken.Token);
            
            // Connect receiving UDP packets.
            var udpClient = this._udpClients[serverLocalPort];
            var _ = Task.Run(async () =>
            {
                try
                {
                    while (this._udpClients.ContainsKey(serverLocalPort))
                    {
                        // Receive data and send it back.
                        var data = await udpClient.ReceiveAsync();
                        var __ = Task.Run(async () => await this.OnUdpReceived(serverLocalPort, data.Buffer));
                    }
                }
                catch (Exception)
                {
                    // Ignore exceptions.
                }
            }, cancellationToken.Token);
            
            // Close the clients.
            trafficClient.Close();
            redirectClient.Close();
            if (this._udpClients.ContainsKey(serverLocalPort))
            {
                // Clear the UDP client.
                // Done after waiting a bit for testing reasons.
                await Task.Delay(500);
                this._udpClients[serverLocalPort].Close();
                this._udpClients.Remove(serverLocalPort);
            }
            cancellationToken.Cancel();
        }

        /// <summary>
        /// Sends UDP from the server client to the connected client.
        /// </summary>
        /// <param name="serverLocalPort">Port of the server connection.</param>
        /// <param name="data">Data to send.</param>
        /// <returns></returns>
        private async Task SendUdp(int serverLocalPort, byte[] data)
        {
            // Send the UDP packet.
            if (this._udpClients.ContainsKey(serverLocalPort))
            {
                await this._udpClients[serverLocalPort].SendAsync(data, data.Length, this._redirectHost, this._redirectPort);
            }
        }
        
        /// <summary>
        /// Invoked when a UDP packet is received.
        /// </summary>
        /// <param name="serverLocalPort">Port of the server connection.</param>
        /// <param name="data">Data that was received.</param>
        /// <returns></returns>
        private async Task OnUdpReceived(int serverLocalPort, byte[] data)
        {
            // Create the payload.
            var portBytes = BitConverter.GetBytes(serverLocalPort);
            var payload = new byte[portBytes.Length + data.Length];
            Array.Copy(portBytes, payload, portBytes.Length);
            Array.Copy(data, 0, payload, portBytes.Length, data.Length);
            
            // Send the packet. 
            await this._stream.SendAsync(new PacketData(PacketType.UdpPacketSent, payload));
        }
    }
}