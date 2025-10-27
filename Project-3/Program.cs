using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using reactive_web_server.HTTP;

namespace reactive_web_server
{
    internal class Program
    {
        private const int Port = 8080;
        private static TcpListener listener;
        
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("Reactive web server started.");
            AppStartup.Initialize();

            listener = new TcpListener(IPAddress.Loopback, Port);
            listener.Start();
            Console.WriteLine($"Listening on http://localhost:{Port}");

            var connectionObservable = Observable.Create<TcpClient>(observer =>
            {
                Task.Factory.StartNew(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            TcpClient client = await listener.AcceptTcpClientAsync();

                            Console.WriteLine($"Client: {client.Client.RemoteEndPoint}");
                            observer.OnNext(client);
                        }
                        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
                        {
                            observer.OnCompleted();
                            break;
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                            break;
                        }
                    }
                }, TaskCreationOptions.LongRunning);

                return Disposable.Create(() => listener.Stop());
            });

            IDisposable subscription = connectionObservable
                .ObserveOn(TaskPoolScheduler.Default)
                .Subscribe(
                    client =>
                    {
                        ClientHandler.HandleClient(client);
                    },
                    ex =>
                    {
                        Console.WriteLine($"Connection Loop Error: {ex.Message}");
                    },
                    () =>
                    {
                        Console.WriteLine("Completed.");
                    });

            // Drži aplikaciju aktivnom dok se ne pritisne Enter
            Console.WriteLine("Press Enter to stop the server...");
            Console.ReadLine();

            // Oslobadjanje resursa
            subscription.Dispose();
            Console.WriteLine("Server stopped.");
        }
    }
}

