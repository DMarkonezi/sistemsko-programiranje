using System;

namespace reactive_web_server.Stats
{
    public class WeatherStats
    {
        public double AverageHumidity { get; set; }
        public double MinHumidity { get; set; }
        public double MaxHumidity { get; set; }

        public double AverageVisibility { get; set; }
        public double MinVisibility { get; set; }
        public double MaxVisibility { get; set; }

        public double AverageUVIndex { get; set; }
        public double MinUVIndex { get; set; }
        public double MaxUVIndex { get; set; }

        public int SampleCount { get; set; }
    }
}
