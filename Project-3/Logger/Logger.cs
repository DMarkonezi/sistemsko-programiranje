using System;

namespace reactive_web_server.Logger
{
    public class RequestLog
    {
        public string ClientIP { get; set; }
        public string Method { get; set; }
        public string Url { get; set; }
        public int StatusCode { get; set; }
        public string StatusText { get; set; }
        public DateTime RequestTime { get; set; }
        public DateTime ResponseTime { get; set; }
        public long ProcessingTimeMs { get; set; }
        public string Error { get; set; }
        public string City { get; set; }

        public override string ToString()
        {
            string errorInfo = string.IsNullOrEmpty(Error) ? "Success" : $"Error: {Error}";
            return $"[{RequestTime:yyyy-MM-dd HH:mm:ss}] {ClientIP} | {Method} {Url} | " +
                   $"Status: {StatusCode} {StatusText} | City: {City ?? "N/A"} | " +
                   $"Time: {ProcessingTimeMs}ms | {errorInfo}";
        }
    }
}
