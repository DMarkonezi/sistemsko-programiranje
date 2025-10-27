using multithreaded_web_server.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using tasks_web_server.Utils;
using System.Threading.Tasks;
using tasks_web_server.Cache;

public enum HttpStatusCode
{
    OK = 200,
    BadRequest = 400,
    NotFound = 404,
    MethodNotAllowed = 405,
    InternalServerError = 500
}

namespace tasks_web_server.HTTP
{
    class HttpRequest
    {
        public string StatusLine;
        public Dictionary<string, string> Headers;
        public string Body;
    }
    static class HTTP_handler
    {
        private static async Task<HttpRequest> ParseHttpRequestAsync(StreamReader reader)
        {
            HttpRequest http_request = new HttpRequest();

            http_request.StatusLine = await reader.ReadLineAsync();
            Console.WriteLine(http_request.StatusLine);
            if (string.IsNullOrEmpty(http_request.StatusLine)) return null;

            http_request.Headers = new Dictionary<string, string>();

            string line;

            while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
            {
                Console.WriteLine(line);
                int colon_index = line.IndexOf(':');
                if (colon_index > 0)
                {
                    string key = line.Substring(0, colon_index).Trim();
                    string value = line.Substring(colon_index + 1).Trim();
                    http_request.Headers[key] = value;
                }
            }

            if (http_request.Headers.TryGetValue("Content-Length", out string lengthStr) && int.TryParse(lengthStr, out int content_length) && content_length > 0)
            {
                char[] read_buffer = new char[content_length];
                int total_read_count = 0;
                while (total_read_count < content_length)
                {
                    int read = await reader.ReadAsync(read_buffer, total_read_count, content_length - total_read_count);
                    if (read == 0) break;
                    total_read_count += read;
                }
                http_request.Body = new string(read_buffer, 0, total_read_count);
            }
            else
            {
                http_request.Body = null;
            }

            return http_request;
        }

        private static String FormatHttpResponse(int http_status_code, String filename = null, String file_content = null,
            String errorMessage = null)
        {
            Dictionary<string, string> response_headers = new Dictionary<string, string>
            {
                { "Date", DateTime.UtcNow.ToString("R") },
                { "Connection", "Close" }
            };

            String body_content = null;
            String body_title = null;
            String body_text = null;
            String http_status_text = null;
            String headers = "";

            switch (http_status_code)
            {
                case 200:
                    http_status_text = "OK";
                    body_title = filename;
                    body_text = file_content;
                    break;
                case 400:
                    http_status_text = "Bad Request";
                    body_title = "Error 400 - Bad Request";
                    body_text = "";
                    break;
                case 404:
                    http_status_text = "Not Found";
                    body_title = "Error 404 - Not Found";
                    body_text = "The file you have been requesting isn't found on the server.";
                    break;
                case 405:
                    http_status_text = "Method Not Allowed";
                    body_title = "Error 405 - Method Not Allowed";
                    body_text = "Method is not supported, this server only supports GET method. Please try again.";
                    break;
                case 500:
                    http_status_text = "Internal Server Error";
                    body_title = "Error 500 - Internal Server Error";
                    body_text = "The server has encountered an error. \n " +
                        errorMessage;
                    break;
                default:
                    Console.WriteLine("Invalid HTTP code in formating function!");
                    return null;
            }

            body_content = $"<html><body><h1>{body_title}</h1><p>{body_text}</p></body></html>";

            foreach (var header in response_headers)
            {
                headers += header.Key.ToString() + ": " + header.Value.ToString() + "\r\n";
            }

            string http_response =
                $"HTTP/1.1 {http_status_code} {http_status_text}\r\n" +
                headers +
                "Content-Type: text/html\r\n" +
                $"Content-Length:  {Encoding.UTF8.GetByteCount(body_content).ToString()};\r\n" +
                "\r\n" +
                body_content;

            return http_response;
        }

        public static async Task HandleClientAsync(object o)
        {
            var args = (Tuple<TcpClient, CacheManager>)o;

            TcpClient client = (TcpClient)args.Item1;
            CacheManager cache = args.Item2;

            NetworkStream stream = client.GetStream();
            StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };
            StreamReader reader = new StreamReader(stream);
            string clientIP = client.Client.RemoteEndPoint.ToString();

            HttpRequest parsed_http_request = await ParseHttpRequestAsync(reader);

            string status_line = parsed_http_request.StatusLine;
            Dictionary<string, string> headers = parsed_http_request.Headers;
            string body_content = parsed_http_request.Body;

            string[] status_line_parts = status_line.Split(' ');
            /*
                0 - Method
                1 - Requested File Nama
                2 - HTTP ver
            */

            string filename = status_line_parts[1].Substring(1);

            string responseToSend; // HTTP Response

            try
            {
                if (status_line_parts[0] != "GET")
                {
                    writer.WriteLine(FormatHttpResponse((int)HttpStatusCode.MethodNotAllowed));
                    await Logger.LogAsync($"[{clientIP}] requested {status_line_parts[0]} {status_line_parts[1]} -> 405 Method Not Allowed");
                    client.Close();
                    return;
                }

                if (string.IsNullOrEmpty(filename))
                {
                    throw new InvalidOperationException("File name is missing in the request path.");
                }

                bool isCached = await cache.TryGetAsync(filename, out string cachedResponse);

                if (isCached)
                {
                    await writer.WriteAsync(cachedResponse);
                    await writer.FlushAsync();
                    await Logger.LogAsync($"[{clientIP}] GET {filename} -> 200 OK [CACHED]");
                    return;
                }

                if (await FileHandler.ConvertFileBinaryTxtAsync(filename))
                {
                    string body = "Conversion finished successfully.";
                    responseToSend = FormatHttpResponse((int)HttpStatusCode.OK, body);
                    await Logger.LogAsync($"[{clientIP}] converted {filename} successfully.");
                }
                else
                {
                    string body = "Conversion has already been done for this file.";
                    responseToSend = FormatHttpResponse((int)HttpStatusCode.OK, body);
                    await Logger.LogAsync($"[{clientIP}] requested {filename} -> already converted.");
                }

                await writer.WriteAsync(responseToSend);
                await writer.FlushAsync();

                await cache.AddAsync(filename, responseToSend);
            }
            catch (FileNotFoundException ex)
            {
                responseToSend = FormatHttpResponse((int)HttpStatusCode.NotFound);
                await writer.WriteLineAsync(responseToSend);
                await Logger.LogAsync($"[{clientIP}] requested {status_line_parts[0]} {status_line_parts[1]} -> 404 Not Found\n" +
                    $"({ex.Message})");

                if (filename.Length > 1)
                    await cache.AddAsync(filename, responseToSend);
            }
            catch (InvalidOperationException ex)
            {
                await writer.WriteLineAsync(FormatHttpResponse((int)HttpStatusCode.BadRequest));
                await Logger.LogAsync($"[{clientIP}] requested {filename} -> 400 Bad Request ({ex.Message})");
            }
            catch (Exception ex)
            {
                await writer.WriteLineAsync(FormatHttpResponse((int)HttpStatusCode.InternalServerError, null, null, ex.Message));
                await Logger.LogAsync($"[{clientIP}] requested {status_line_parts[1]} -> 500 Internal Server Error " +
                    $"({ex.Message})");
            }
            finally
            {
                if (writer != null) writer.Dispose();
                if (reader != null) reader.Dispose();
                if (stream != null) stream.Close();
                if (client != null) client.Close();
            }
        }
    }
}
