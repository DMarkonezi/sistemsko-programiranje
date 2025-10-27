using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace reactive_web_server.Weather
{
    public class WeatherData
    {
        public double Humidity { get; set; }
        public double Visibility { get; set; }
        public double UVIndex { get; set; }
        public DateTime Timestamp { get; set; }

        public WeatherData(double humidity, double visibility, double uvIndex, DateTime timestamp)
        {
            Humidity = humidity;
            Visibility = visibility;
            UVIndex = uvIndex;
            Timestamp = timestamp;
        }
    }
}
