using System;
using System.IO;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading.Tasks;
using reactive_web_server.Logger;
using reactive_web_server.Reactive;
using reactive_web_server.Stats;

namespace reactive_web_server.HTTP
{
    

    static class ClientHandler
    {
        private static void LogRequest(RequestLog log)
        {
            Console.WriteLine(log.ToString());
        }

        public static void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };
            StreamReader reader = new StreamReader(stream);
            var clientIP = client.Client.RemoteEndPoint.ToString();

            DateTime requestStartTime = DateTime.Now;
            RequestLog log = new RequestLog { ClientIP = clientIP, RequestTime = requestStartTime };

            try
            {
                var request = HttpHandler.Parse_HTTP_Request(reader);

                if (request == null)
                {
                    log.StatusCode = (int)HttpStatusCode.BadRequest;
                    log.StatusText = "Bad Request";
                    log.Error = "Invalid HTTP request format";
                    string badRequest = HttpHandler.Format_HTTP_response((int)HttpStatusCode.BadRequest, log.Error);
                    writer.Write(badRequest);
                    log.ResponseTime = DateTime.Now;
                    log.ProcessingTimeMs = (long)(log.ResponseTime - requestStartTime).TotalMilliseconds;
                    LogRequest(log);
                    return;
                }

                var statusLineParts = request.StatusLine.Split(' ');
                log.Method = statusLineParts.Length > 0 ? statusLineParts[0] : "UNKNOWN";
                log.Url = statusLineParts.Length > 1 ? statusLineParts[1] : "UNKNOWN";

                if (statusLineParts.Length < 2 || statusLineParts[0] != "GET")
                {
                    log.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    log.StatusText = "Method Not Allowed";
                    log.Error = $"Method {log.Method} not supported";
                    string notAllowed = HttpHandler.Format_HTTP_response((int)HttpStatusCode.MethodNotAllowed);
                    writer.Write(notAllowed);
                    log.ResponseTime = DateTime.Now;
                    log.ProcessingTimeMs = (long)(log.ResponseTime - requestStartTime).TotalMilliseconds;
                    LogRequest(log);
                    return;
                }

                string url = statusLineParts[1];

                if (!url.StartsWith("/weather"))
                {
                    log.StatusCode = (int)HttpStatusCode.NotFound;
                    log.StatusText = "Not Found";
                    log.Error = $"Endpoint {url} not found";
                    string notFound = HttpHandler.Format_HTTP_response((int)HttpStatusCode.NotFound);
                    writer.Write(notFound);
                    log.ResponseTime = DateTime.Now;
                    log.ProcessingTimeMs = (long)(log.ResponseTime - requestStartTime).TotalMilliseconds;
                    LogRequest(log);
                    return;
                }

                string city = null;
                if (url.Contains("?"))
                {
                    string[] urlParts = url.Split('?');
                    string query = urlParts[1];
                    var queryParams = query.Split('&');

                    foreach (var param in queryParams)
                    {
                        var kv = param.Split('=');
                        if (kv.Length == 2 && kv[0] == "city")
                        {
                            city = Uri.UnescapeDataString(kv[1]);
                            break;
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(city))
                {
                    log.StatusCode = (int)HttpStatusCode.BadRequest;
                    log.StatusText = "Bad Request";
                    log.Error = "Missing city parameter";
                    string badRequest = HttpHandler.Format_HTTP_response((int)HttpStatusCode.BadRequest, "Missing city parameter");
                    writer.Write(badRequest);
                    log.ResponseTime = DateTime.Now;
                    log.ProcessingTimeMs = (long)(log.ResponseTime - requestStartTime).TotalMilliseconds;
                    LogRequest(log);
                    return;
                }

                log.City = city;

                AppStartup.WeatherStatsService.ResetStats();

                // 

                var weatherObservable = Observable.FromAsync<WeatherStats>(async ct =>
                {
                    try
                    {
                        LocationStream.PushLocation(city);
                        await Task.Delay(500, ct);
                        var stats = AppStartup.WeatherStatsService.GetCurrentWeatherStats();
                        return stats;
                    }
                    catch (OperationCanceledException)
                    {
                        throw new Exception("Weather request cancelled");
                    }
                });

                weatherObservable.Subscribe(
                    onNext: stats =>
                    {
                        try
                        {
                            string html = HttpHandler.Format_Stats_Html(stats);
                            string response = HttpHandler.Format_HTTP_response((int)HttpStatusCode.OK, city, html);
                            writer.Write(response);

                            // Log uspesne obrade
                            log.StatusCode = (int)HttpStatusCode.OK;
                            log.StatusText = "OK";
                            log.ResponseTime = DateTime.Now;
                            log.ProcessingTimeMs = (long)(log.ResponseTime - requestStartTime).TotalMilliseconds;
                            LogRequest(log);
                        }
                        catch (Exception ex)
                        {
                            HandleError(ex, writer, requestStartTime, log);

                        }
                        finally
                        {
                            writer.Flush();
                            stream.Close();
                            client.Close();
                        }
                    },
                    onError: ex =>
                    {
                        try
                        {
                            /*log.StatusCode = (int)HttpStatusCode.InternalServerError;
                            log.StatusText = "Internal Server Error";
                            log.Error = ex.Message;
                            string error = HttpHandler.Format_HTTP_response((int)HttpStatusCode.InternalServerError, "Weather Error", ex.Message);
                            writer.Write(error);
                            log.ResponseTime = DateTime.Now;
                            log.ProcessingTimeMs = (long)(log.ResponseTime - requestStartTime).TotalMilliseconds;
                            LogRequest(log);*/
                            HandleError(ex, writer, requestStartTime, log);
                        }
                        catch (Exception writeEx)
                        {
                            Console.WriteLine($"[Error] Could not write eror response: {writeEx.Message}");
                        }
                        finally
                        {
                            writer.Flush();
                            stream.Close();
                            client.Close();
                        }
                    });
            }
            catch (Exception ex)
            {
                HandleError(ex, writer, requestStartTime, log);
                stream.Close();
                client.Close();
            }
        }

        private static void HandleError(Exception ex, StreamWriter writer, DateTime requestStartTime, RequestLog log)
        {
            log.StatusCode = (int)HttpStatusCode.InternalServerError;
            log.StatusText = "Internal Server Error";
            log.Error = ex.Message;
            string error = HttpHandler.Format_HTTP_response((int)HttpStatusCode.InternalServerError, "Server Error", ex.Message);
            writer.Write(error);
            log.ResponseTime = DateTime.Now;
            log.ProcessingTimeMs = (long)(log.ResponseTime - requestStartTime).TotalMilliseconds;
            LogRequest(log);
        }
    }
}