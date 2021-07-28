/*
 * TheNexusAvenger
 *
 * Runs the server application.
 */

using System.CommandLine;
using System.CommandLine.Invocation;
using NexusRelay;
using NexusRelayServer.Server;

namespace NexusRelayServer
{
    class Program
    {
        public static int Main(string[] args)
        {
            // Create the root command for parsing arguments.
            var rootCommand = new RootCommand
            {
                new Option<int>(
                    "--port",
                    "Port to host in the main management port."),
                new Option<string>(
                    "--secret",
                    "Secret required by the Nexus Relay client to start accepting traffic."),
            };
            rootCommand.Name = "NexusRelayServer";
            rootCommand.Description = "TCP/UDP traffic forwarder application for exposing services.";

            // Set up the handler.
            rootCommand.Handler = CommandHandler.Create<int, string>((port, secret) =>
            {
                // Return if a value is missing.
                if (port == default)
                {
                    Logger.Error("Port is not defined.");
                    return 1;
                }
                if (secret == default)
                {
                    Logger.Error("Secret is not defined.");
                    return 1;
                }
                
                // Create the host server.
                var server = new HostServer(port, secret);
                
                // Start the host server.
                Logger.Info($"Serving host server on port {port}");
                server.StartAsync().Wait();
                return 0;
            });

            // Invoke the command and return the response code.
            return rootCommand.InvokeAsync(args).Result;
        }
    }
}