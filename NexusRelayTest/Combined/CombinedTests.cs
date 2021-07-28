/*
 * TheNexusAvenger
 *
 * Tests replicating data from a server,
 * including TCP and UDP. 
 */

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NexusRelay.Extension;
using NexusRelayClient;
using NexusRelayServer.Server;
using NUnit.Framework;

namespace NexusRelayTest.Combined
{
    public class CombinedTests
    {
        /// <summary>
        /// Returns a random port to use for testing.
        /// </summary>
        /// <returns>Random port to use.</returns>
        public int GetRandomPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint) listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        /// <summary>
        /// Sends a test request to the test server.
        /// </summary>
        /// <param name="serverPort">Port of the test server.</param>
        /// <param name="receivePort">Client port to receive from.</param>
        /// <param name="number">2-digit number to send.</param>
        /// <returns></returns>
        public string SendTestTCPMessage(int serverPort, int receivePort, string number)
        {
            // Send the request with the 2-digit number.
            var testConnection = new TcpClient(new IPEndPoint(IPAddress.Loopback, receivePort));
            testConnection.Connect("127.0.0.1", serverPort);
            var testStream = testConnection.GetStream();
            testStream.Write(Encoding.ASCII.GetBytes(number));
            
            // Read the response.
            var response = new byte[17];
            testStream.Read(response);
            testConnection.Close();
            return Encoding.ASCII.GetString(response);
        }

        /// <summary>
        /// Sends a test request to the test UDP server.
        /// </summary>
        /// <param name="serverPort">Port of the test server.</param>
        /// <param name="receivePort">Client port to receive from.</param>
        /// <param name="number">2-digit number to send.</param>
        /// <returns></returns>
        public string SendTestUDPMessage(int serverPort, int receivePort, string number)
        {
            // Send the request with the 2-digit number.
            var serverAddress = new IPEndPoint(IPAddress.Loopback, serverPort);
            var clientAddress = new IPEndPoint(IPAddress.Loopback, receivePort);
            var testConnection = new UdpClient(receivePort);
            testConnection.Send(Encoding.ASCII.GetBytes(number), 2, serverAddress);
            
            // Read a response.
            var response = testConnection.Receive(ref clientAddress);
            testConnection.Close();
            return Encoding.ASCII.GetString(response);
        }
        
        [Test]
        public void TestCombined()
        {
             // Create a TCP and UDP listener to act as a source.
             var testClientPort = GetRandomPort();
             var hostManagePort = GetRandomPort();
             var hostServerPort = GetRandomPort();
             var sourceTcpListener = new TcpListener(IPAddress.Loopback, 0);
             sourceTcpListener.Start();
             var testServerPort = ((IPEndPoint) sourceTcpListener.LocalEndpoint).Port;
             var sourceUdpListener = new UdpClient(testServerPort);
             
             // Accept traffic in a thread.
             new Thread(() =>
             {
                 try
                 {
                     while (true)
                     {
                         var connection = sourceTcpListener.AcceptTcpClient();
                         Console.WriteLine("Accepted TCP connection from remote port " + connection.GetRemoteEndPoint().Port + " to local port " + testServerPort);
                         var stream = connection.GetStream();
                         var numberResponse = new byte[2];
                         stream.Read(numberResponse);
                         stream.Write(Encoding.ASCII.GetBytes("TestTCPResponse" + Encoding.ASCII.GetString(numberResponse)));
                         connection.Close();
                         Console.WriteLine("Sent TCP response.");
                     }
                 }
                 catch (Exception)
                 {
                     // Closed by the test.
                 }
                 Console.WriteLine("Stopped test TCP server.");
             }).Start();
             new Thread(async () =>
             {
                 while (true)
                 {
                     try
                     {

                         var response = await sourceUdpListener.ReceiveAsync();
                         Console.WriteLine("Accepted UDP message from remote port " + ((IPEndPoint) response.RemoteEndPoint).Port + " to local port " + testServerPort);
                         await sourceUdpListener.SendAsync(Encoding.ASCII.GetBytes("TestUDPResponse" + Encoding.ASCII.GetString(response.Buffer)), 17, response.RemoteEndPoint);
                         Console.WriteLine("Sent UDP response.");
                     }
                     catch (SocketException)
                     {
                         // Closed by the test.
                     }
                 }
             }).Start();

             // Assert the base server is valid.
             Assert.AreEqual(this.SendTestTCPMessage(testServerPort, testClientPort, "00"), "TestTCPResponse00", "TCP response is invalid (test invalid).");
             Assert.AreEqual(this.SendTestUDPMessage(testServerPort, testClientPort, "01"), "TestUDPResponse01", "UDP response is invalid (test invalid).");
             
             // Set up the traffic forwarder server.
             var hostServer = new HostServer(hostManagePort, "TestSecret");
             Task.Run(hostServer.StartAsync);
             
             // Wait to continue to make sure the server is started.
             Thread.Sleep(100);
             
             // Set up the traffic forwarder client.
             var client = new Client("127.0.0.1", hostManagePort, hostServerPort, "127.0.0.1", testServerPort);
             Task.Run(() => client.StartConnectingAsync("TestSecret"));
             
             // Wait to continue to make sure the client is started.
             Thread.Sleep(100);
             
             // Send a test message over TCP and UDP.
             Assert.AreEqual(this.SendTestTCPMessage(hostServerPort, testClientPort, "02"), "TestTCPResponse02");
             Assert.AreEqual(this.SendTestUDPMessage(hostServerPort, testClientPort, "03"), "TestUDPResponse03");

             // Stop the servers.
             sourceTcpListener.Stop();
             sourceUdpListener.Close();
        }
    }
}