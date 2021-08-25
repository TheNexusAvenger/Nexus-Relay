using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Nexus.Logging.Output;

namespace NexusRelay
{
    public static class Logger
    {
        /// <summary>
        /// Nexus Logger instance used.
        /// </summary>
        private static readonly Nexus.Logging.Logger NexusLogger = new Nexus.Logging.Logger();

        /// <summary>
        /// Console output of the application.
        /// </summary>
        private static readonly ConsoleOutput ConsoleOutput = new ConsoleOutput()
        {
            IncludeDate = true,
            NamespaceWhitelist = new List<string>() { "NexusRelay" },
            MinimumLevel = LogLevel.Information,
        };
        
        /// <summary>
        /// File output of the application.
        /// </summary>
        private static readonly FileOutput FileOutput = new FileOutput()
        {
            IncludeDate = true,
            NamespaceWhitelist = new List<string>() { "NexusRelay" },
            MinimumLevel = LogLevel.None,
            FileLocation = "NexusRelay.log",
        };

        /// <summary>
        /// Sets the console log level.
        /// </summary>
        /// <param name="level">Level to set to.</param>
        public static void SetConsoleLogLevel(string level)
        {
            if (Enum.TryParse<LogLevel>(level, out var logLevel))
            {
                ConsoleOutput.MinimumLevel = logLevel;
            }
            else
            {
                Warning("Console log level \"" + level + "\" invalid. Defaulting to " + ConsoleOutput.MinimumLevel);
            }
        }
        
        /// <summary>
        /// Sets the file log level.
        /// </summary>
        /// <param name="level">Level to set to.</param>
        public static void SetFileLogLevel(string level)
        {
            if (Enum.TryParse<LogLevel>(level, out var logLevel))
            {
                FileOutput.MinimumLevel = logLevel;
            }
            else
            {
                Warning("File log level \"" + level + "\" invalid. Defaulting to " + FileOutput.MinimumLevel);
            }
        }

        /// <summary>
        /// Sets the log file location.
        /// </summary>
        /// <param name="location">Location of the log file.</param>
        public static void SetLogFile(string location)
        {
            FileOutput.FileLocation = location;
        }

        /// <summary>
        /// Initializes the logger.
        /// </summary>
        static Logger()
        {
            NexusLogger.Outputs.Add(ConsoleOutput);
            NexusLogger.Outputs.Add(FileOutput);
        }
        
        /// <summary>
        /// Writes an debug message.
        /// </summary>
        /// <param name="message">Message to write.</param>
        public static void Debug(string message)
        {
            NexusLogger.Debug(message);
        }
        
        /// <summary>
        /// Writes an information message.
        /// </summary>
        /// <param name="message">Message to write.</param>
        public static void Info(string message)
        {
            NexusLogger.Info(message);
        }
        
        /// <summary>
        /// Writes an warning message.
        /// </summary>
        /// <param name="message">Message to write.</param>
        public static void Warning(string message)
        {
            NexusLogger.Warn(message);
        }
        
        /// <summary>
        /// Writes an error message.
        /// </summary>
        /// <param name="message">Message to write.</param>
        public static void Error(string message)
        {
            NexusLogger.Error(message);
        }
    }
}