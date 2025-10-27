using multithreaded_web_server.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using tasks_web_server.HTTP;
using tasks_web_server.Cache;

namespace tasks_web_server
{
    internal class Program
    {
        private static readonly int PORT = 5050;

        private static CacheManager cache = new CacheManager(
            maxItems: 50,
            expMinutes: 5
        );

        static async Task Main(string[] args)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, PORT);
            listener.Start();
            await Logger.ServerStartLogInformationAsync(PORT);

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                var arguments = Tuple.Create(client, cache);

                _ = HTTP_handler.HandleClientAsync(arguments);
            }
        }
    }
}
