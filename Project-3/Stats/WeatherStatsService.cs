using System;
using System.Collections.Generic;
using System.Linq;
using reactive_web_server.Weather;

namespace reactive_web_server.Stats
{
    public class WeatherStatsService
    {
        private readonly List<WeatherData> dataPoints = new List<WeatherData>();
        
        private WeatherStats currentStats = new WeatherStats();

        public void UpdateWeatherStats(List<WeatherData> newDataPoints)
        {
            if (newDataPoints == null || newDataPoints.Count == 0)
            {
                Console.WriteLine("[WeatherStatsService] Received empty data");
                return;
            }

            dataPoints.Clear();
            dataPoints.AddRange(newDataPoints);  // Store them

            var humidities = dataPoints.Select(d => d.Humidity);
            var visibilities = dataPoints.Select(d => d.Visibility);
            var uvIndexes = dataPoints.Select(d => d.UVIndex);

            currentStats = new WeatherStats
            {
                AverageHumidity = humidities.Average(),
                MinHumidity = humidities.Min(),
                MaxHumidity = humidities.Max(),

                AverageVisibility = visibilities.Average(),
                MinVisibility = visibilities.Min(),
                MaxVisibility = visibilities.Max(),

                AverageUVIndex = uvIndexes.Average(),
                MinUVIndex = uvIndexes.Min(),
                MaxUVIndex = uvIndexes.Max(),

                SampleCount = dataPoints.Count
            };

            Console.WriteLine($"[WeatherStatsService] Updated: {dataPoints.Count} data points");
        }

        public WeatherStats GetCurrentWeatherStats()
        {
            return currentStats;
        }

        public void ResetStats()
        {
            lock (dataPoints)
            {
                dataPoints.Clear();
                currentStats = new WeatherStats();
            }
        }
    }
}
