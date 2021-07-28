using System;
using System.Globalization;

namespace NexusRelay
{
    public static class Logger
    {
        /// <summary>
        /// Writes an debug message.
        /// </summary>
        /// <param name="message">Message to write.</param>
        public static void Debug(string message)
        {
            WriteMessage(message, "Debug", ConsoleColor.Green);
        }
        
        /// <summary>
        /// Writes an information message.
        /// </summary>
        /// <param name="message">Message to write.</param>
        public static void Info(string message)
        {
            WriteMessage(message, "Information", ConsoleColor.White);
        }
        
        /// <summary>
        /// Writes an warning message.
        /// </summary>
        /// <param name="message">Message to write.</param>
        public static void Warning(string message)
        {
            WriteMessage(message, "Warning", ConsoleColor.Yellow);
        }
        
        /// <summary>
        /// Writes an error message.
        /// </summary>
        /// <param name="message">Message to write.</param>
        public static void Error(string message)
        {
            WriteMessage(message, "Error", ConsoleColor.Red);
        }
        
        /// <summary>
        /// Writes a message to the console.
        /// </summary>
        /// <param name="message">Message to write.</param>
        /// <param name="type">Type of the message.</param>
        /// <param name="color">Color of the message.</param>
        private static void WriteMessage(string message, string type, ConsoleColor color)
        {
            // Set up the message.
            var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            Console.ForegroundColor = color;
            
            // Write the messages.
            foreach (var line in message.Split("\n"))
            {
                Console.WriteLine("[" + date + "] [" + type + "]: " + line);
            }
        }
    }
}