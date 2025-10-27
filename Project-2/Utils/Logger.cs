using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace multithreaded_web_server.Utils
{
    static class Logger
    {
        private static readonly object logLock = new object();
        private static readonly string logFilePath = "../../Logs/server.log";

        public static async Task ServerStartLogInformationAsync(int port)
        {
            string separator = new string('=', 100);
            string timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            string hostname = Dns.GetHostName();
            string ipAddress = Dns.GetHostEntry(hostname)
                                  .AddressList
                                  .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                                  ?.ToString() ?? "Unknown";

            string message =
                separator + "\n" +
                $"[{timestamp}] Server started\n" +
                $"  Hostname     : {hostname}\n" +
                $"  IP Address   : {ipAddress}\n" +
                $"  Listening on : port {port}\n" +
                $"  Runtime      : {Environment.Version}\n" +
                $"  OS           : {Environment.OSVersion}\n";

            LogSync(message);
        }

        private static void LogSync(string message)
        {
            String logMessage = $"[{DateTime.Now:HH:mm:ss.fff}" + $"] " + message + "\r\n";

            lock (logLock)
            {
                File.AppendAllText(logFilePath, logMessage);
            }
        }

        public static Task LogAsync(string message)
        {
            return Task.Run(() => LogSync(message));
        }
    }
}
