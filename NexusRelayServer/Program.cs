/*
 * TheNexusAvenger
 *
 * Runs the server application.
 */

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using Nexus.Logging.Output;
using NexusRelay;
using NexusRelayServer.Server;

namespace NexusRelayServer
{
    public class ClientInputs
    {
        public int Port { get; set; }
        public string Secret { get; set; }
        public string ConsoleLogLevel { get; set; }
        public string FileLogLevel { get; set; }
        public string LogFile { get; set; }
    }
    
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
                new Option<string>(
                    "--console-log-level",
                    "Log level of the console output."),
                new Option<string>(
                    "--file-log-level",
                    "Log level of the file output."),
                new Option<string>(
                    "--log-file",
                    "File location of the logs."),
            };
            rootCommand.Name = "NexusRelayServer";
            rootCommand.Description = "TCP/UDP traffic forwarder application for exposing services.";

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
                if (clientInputs.LogFile != default)
                {
                    Logger.SetLogFile(clientInputs.LogFile);
                }

                // Return if a value is missing.
                if (clientInputs.Port == default)
                {
                    Logger.Error("Port is not defined.");
                    return 1;
                }
                if (clientInputs.Secret == default)
                {
                    Logger.Error("Secret is not defined.");
                    return 1;
                }
                
                // Create the host server.
                var server = new HostServer(clientInputs.Port, clientInputs.Secret);
                
                // Start the host server.
                Logger.Info($"Serving host server on port {clientInputs.Port}");
                server.StartAsync().Wait();
                return 0;
            });

            // Invoke the command and return the response code.
            return rootCommand.InvokeAsync(args).Result;
        }
    }
}