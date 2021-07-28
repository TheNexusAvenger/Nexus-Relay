/*
 * TheNexusAvenger
 *
 * Extensions for the TcpClient class.
 */

using System.Net;
using System.Net.Sockets;

namespace NexusRelay.Extension
{
    public static class TcpClientExtensions
    {
        /// <summary>
        /// Returns the local endpoint of the TCP client as an IPEndPoint.
        /// </summary>
        /// <returns>The local endpoint of the TCP client as an IPEndPoint.</returns>
        public static IPEndPoint GetLocalEndPoint(this TcpClient @this)
        {
            return (IPEndPoint) @this.Client.LocalEndPoint;
        }
        
        /// <summary>
        /// Returns the remote endpoint of the TCP client as an IPEndPoint.
        /// </summary>
        /// <returns>The remote endpoint of the TCP client as an IPEndPoint.</returns>
        public static IPEndPoint GetRemoteEndPoint(this TcpClient @this)
        {
            return (IPEndPoint) @this.Client.RemoteEndPoint;
        }
    }
}