/*
 * TheNexusAvenger
 *
 * Hosts the server for forwarding traffic.
 */

using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NexusRelay;
using NexusRelay.Replication;

namespace NexusRelayServer.Server
{
    public class HostServer
    {
        /// <summary>
        /// Listener for accepting traffic forwarders.
        /// </summary>
        private readonly TcpListener _listener;
        
        /// <summary>
        /// Secret for accepting connections.
        /// </summary>
        private readonly string _secret;
        
        /// <summary>
        /// Traffic forwarders that are connected.
        /// </summary>
        private readonly Dictionary<int, HostInstance> _hosts = new Dictionary<int, HostInstance>();
        
        /// <summary>
        /// Creates the server.
        /// </summary>
        /// <param name="hostPort">Port to run the manage port.</param>
        /// <param name="secret">Secret required by the client to authenticate.</param>
        public HostServer(int hostPort, string secret)
        {
            // Create the listener.
            this._listener = new TcpListener(IPAddress.Any, hostPort);
            this._secret = secret;
        }
        
        /// <summary>
        /// Starts listening on the server.
        /// </summary>
        public async Task StartAsync()
        {
            // Start the listener.
            this._listener.Start();
            
            // Start accepting connections.
            while (true)
            {
                // Listen for a connection.
                var connection = await this._listener.AcceptTcpClientAsync();
                
                // Start the connection.
                var _ = Task.Run(async () =>
                {
                    await this.ProcessConnectionAsync(connection);
                });
            }
        }
        
        /// <summary>
        /// Starts processing a connection.
        /// </summary>
        /// <param name="client">Client that is attempting to set up traffic forwarding.</param>
        private async Task ProcessConnectionAsync(TcpClient client)
        {
            // Close the connection if the secret doesn't match.
            var stream = client.GetStream();
            var packetStream = new PacketStream(stream);
            var secret = new byte[this._secret.Length];
            await stream.ReadAsync(secret);
            if (Encoding.ASCII.GetString(secret) != this._secret)
            {
                client.Close();
                return;
            }
            
            // Get the intended server port.
            var portRequestPacket = await packetStream.ReceiveAsync();
            if (portRequestPacket.Type != PacketType.RequestPort)
            {
                client.Close();
                return;
            }
            var localPort = int.Parse(portRequestPacket.GetPayload());
            
            // Close the existing server instance.
            if (this._hosts.ContainsKey(localPort))
            {
                this._hosts[localPort].Close();
                this._hosts[localPort] = null;
                Logger.Warning($"Shutting down existing port {localPort}");
            }
            Logger.Info($"Starting accepting of traffic through: {localPort}");
            
            // Create the server instance.
            var hostInstance = new HostInstance(localPort, client);
            this._hosts[localPort] = hostInstance;
            await hostInstance.StartAsync();
            
            // Close the servers.
            if (this._hosts.ContainsKey(localPort) && this._hosts[localPort] == hostInstance)
            {
                Logger.Warning($"Shutting down port {localPort}");
                hostInstance.Close();
                this._hosts.Remove(localPort);
            }
        }
    }
}