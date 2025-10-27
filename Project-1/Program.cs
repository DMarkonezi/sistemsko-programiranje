using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using multithreaded_web_server.HTTP;
using multithreaded_web_server.Utils;
using multithreaded_web_server.Cache;

namespace multithreaded_web_server
{
    internal class Program
    {
        private static readonly int PORT = 5050;
        private static CacheManager cache = new CacheManager(
            maxItems: 50,
            expMinutes: 5
        );

        static void Main(string[] args)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, PORT);
            listener.Start();
            Logger.ServerStartLogInformation(PORT);

            while (true) {
                TcpClient client = listener.AcceptTcpClient();
                var arguments = Tuple.Create(client, cache);
                ThreadPool.QueueUserWorkItem(HTTP_handler.HandleClient, arguments);
            }
        }
    }
}
