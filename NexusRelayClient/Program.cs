/*
 * TheNexusAvenger
 *
 * Runs the client application.
 */

using System.CommandLine;
using System.CommandLine.Invocation;
using NexusRelay;

namespace NexusRelayClient
{
    public class Program
    {
        public static int Main(string[] args)
        {
            // Create the root command for parsing arguments.
            var rootCommand = new RootCommand
            {
                new Option<string>(
                    "--remote-host",
                    "Remote management host that hosts Nexus Relay."),
                new Option<int>(
                    "--remote-port",
                    "Remote management host that port Nexus Relay."),
                new Option<int>(
                    "--port",
                    "Port to host on the server."),
                new Option<string>(
                    "--redirect-host",
                    "Host to redirect traffic to. If not specified, 127.0.0.1 is used."),
                new Option<int>(
                    "--redirect-port",
                    "Port to redirect traffic to."),
                new Option<string>(
                    "--secret",
                    "Secret required by the Nexus Relay server to start accepting traffic."),
            };
            rootCommand.Name = "NexusRelayClient";
            rootCommand.Description = "Client for connecting and exposing services with Nexus Relay.";

            // Set up the handler.
            rootCommand.Handler = CommandHandler.Create<string, int, int, string, int, string>((remoteHost, remotePort, port, redirectHost, redirectPort, secret) =>
            {
                // Return if a value is missing.
                if (remoteHost == default)
                {
                    Logger.Error("Remote host is not defined.");
                    return 1;
                }
                if (remotePort == default)
                {
                    Logger.Error("Remote port is not defined.");
                    return 1;
                }
                if (port == default)
                {
                    Logger.Error("Port is not defined.");
                    return 1;
                }
                redirectHost ??= "127.0.0.1";
                if (redirectPort == default)
                {
                    Logger.Error("Redirect port is not defined.");
                    return 1;
                }
                if (secret == default)
                {
                    Logger.Error("Secret is not defined.");
                    return 1;
                }
                
                // Create the client and start serving connections.
                var client = new Client(remoteHost, remotePort, port, redirectHost, redirectPort);
                client.StartConnectingAsync(secret).Wait();
                return 0;
            });

            // Invoke the command and return the response code.
            return rootCommand.InvokeAsync(args).Result;
        }
    }
}