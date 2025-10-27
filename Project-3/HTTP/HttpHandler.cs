using reactive_web_server.Stats;
using System.Collections.Generic;
using System;
using System.Text;
using System.IO;

namespace reactive_web_server.HTTP
{
    public enum HttpStatusCode
    {
        OK = 200,
        BadRequest = 400,
        NotFound = 404,
        MethodNotAllowed = 405,
        InternalServerError = 500
    }

    public class HttpRequest
    {
        public string StatusLine;
        public Dictionary<string, string> Headers;
        public string Body;
    }

    public static class HttpHandler
    {
        public static HttpRequest Parse_HTTP_Request(StreamReader reader)
        {
            HttpRequest http_request = new HttpRequest();

            http_request.StatusLine = reader.ReadLine();
            if (string.IsNullOrEmpty(http_request.StatusLine)) return null;

            http_request.Headers = new Dictionary<string, string>();
            string line;

            while (!string.IsNullOrEmpty(line = reader.ReadLine()))
            {
                int colon_index = line.IndexOf(':');
                if (colon_index > 0)
                {
                    string key = line.Substring(0, colon_index).Trim();
                    string value = line.Substring(colon_index + 1).Trim();
                    http_request.Headers[key] = value;
                }
            }

            if (http_request.Headers.TryGetValue("Content-Length", out string lengthStr) &&
                int.TryParse(lengthStr, out int content_length) && content_length > 0)
            {
                char[] read_buffer = new char[content_length];
                int total_read_count = 0;
                while (total_read_count < content_length)
                {
                    int read = reader.Read(read_buffer, total_read_count, content_length - total_read_count);
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

        public static string Format_HTTP_response(int http_status_code, string filename = null, string file_content = null)
        {
            Dictionary<string, string> response_headers = new Dictionary<string, string>
            {
                { "Date", DateTime.UtcNow.ToString("R") },
                { "Connection", "Close" },
                { "Server", "Reactive-WebServer/1.0" }
            };

            string body_content = null;
            string body_title = null;
            string body_text = null;
            string http_status_text = null;
            string headers = "";

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
                    body_text = filename ?? "";
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
                    body_text = filename ?? "The server has encountered an error.";
                    break;
                default:
                    return null;
            }

            body_content = $"<html><body><h1>{body_title}</h1><p>{body_text}</p></body></html>";

            foreach (var header in response_headers)
            {
                headers += header.Key + ": " + header.Value + "\r\n";
            }

            string http_response =
                $"HTTP/1.1 {http_status_code} {http_status_text}\r\n" +
                headers +
                "Content-Type: text/html; charset=utf-8\r\n" +
                $"Content-Length: {Encoding.UTF8.GetByteCount(body_content)}\r\n" +
                "\r\n" +
                body_content;

            return http_response;
        }

        public static string Format_Stats_Html(WeatherStats stats)
        {
            if (stats == null || stats.SampleCount == 0)
            {
                return "<html><body><h1>No data available yet.</h1></body></html>";
            }

            var sb = new StringBuilder();
            sb.AppendLine("<html><body>");
            sb.AppendLine("<h1>Weather Stats</h1>");
            sb.AppendLine("<ul>");
            sb.AppendLine($"<li>Samples Count: {stats.SampleCount}</li>");
            sb.AppendLine($"<li>Average Humidity: {stats.AverageHumidity:F2}%</li>");
            sb.AppendLine($"<li>Min Humidity: {stats.MinHumidity}%</li>");
            sb.AppendLine($"<li>Max Humidity: {stats.MaxHumidity}%</li>");
            sb.AppendLine($"<li>Average Visibility: {stats.AverageVisibility:F2} m</li>");
            sb.AppendLine($"<li>Min Visibility: {stats.MinVisibility}</li>");
            sb.AppendLine($"<li>Max Visibility: {stats.MaxVisibility}</li>");
            sb.AppendLine($"<li>Average UV Index: {stats.AverageUVIndex:F2}</li>");
            sb.AppendLine($"<li>Min UV Index: {stats.MinUVIndex}</li>");
            sb.AppendLine($"<li>Max UV Index: {stats.MaxUVIndex}</li>");
            sb.AppendLine("</ul>");
            sb.AppendLine("</body></html>");

            return sb.ToString();
        }
    }
}
