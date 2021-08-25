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
    public class ClientInputs
    {
        public string RemoteHost { get; set; }
        public int RemotePort { get; set; }
        public int Port { get; set; }
        public string RedirectHost { get; set; }
        public int RedirectPort { get; set; }
        public string Secret { get; set; }
        public string ConsoleLogLevel { get; set; }
        public string FileLogLevel { get; set; }
    }
    
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
                new Option<string>(
                    "--console-log-level",
                    "Log level of the console output."),
                new Option<string>(
                    "--file-log-level",
                    "Log level of the file output."),
            };
            rootCommand.Name = "NexusRelayClient";
            rootCommand.Description = "Client for connecting and exposing services with Nexus Relay.";

            // Set up the handler.
            rootCommand.Handler = CommandHandler.Create<ClientInputs>((clientInputs) =>
            {
                // Set up the logging.
                if (clientInputs.ConsoleLogLevel != default)
                {
                    Logger.SetConsoleLogLevel(clientInputs.ConsoleLogLevel);
                }
                if (clientInputs.FileLogLevel != default)
                {
                    Logger.SetFileLogLevel(clientInputs.FileLogLevel);
                }
                
                // Return if a value is missing.
                if (clientInputs.RemoteHost == default)
                {
                    Logger.Error("Remote host is not defined.");
                    return 1;
                }
                if (clientInputs.RemotePort == default)
                {
                    Logger.Error("Remote port is not defined.");
                    return 1;
                }
                if (clientInputs.Port == default)
                {
                    Logger.Error("Port is not defined.");
                    return 1;
                }
                clientInputs.RedirectHost ??= "127.0.0.1";
                if (clientInputs.RedirectPort == default)
                {
                    Logger.Error("Redirect port is not defined.");
                    return 1;
                }
                if (clientInputs.Secret == default)
                {
                    Logger.Error("Secret is not defined.");
                    return 1;
                }
                
                // Create the client and start serving connections.
                var client = new Client(clientInputs.RemoteHost, clientInputs.RemotePort, clientInputs.Port, clientInputs.RedirectHost, clientInputs.RedirectPort);
                client.StartConnectingAsync(clientInputs.Secret).Wait();
                return 0;
            });

            // Invoke the command and return the response code.
            return rootCommand.InvokeAsync(args).Result;
        }
    }
}