using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using reactive_web_server.Weather;
using System.Collections.Generic;
using System.Linq;

namespace reactive_web_server.Weather
{
    public class WeatherService
    {
        private readonly HttpClient httpClient = new HttpClient();

        private async Task<(double? Latitude, double? Longitude)> GetCityCoordinatesAsync(string cityName)
        {
            Console.WriteLine($"Ovde mora da je provera, inace salje dobri rezultati: ({cityName})");

            string url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(cityName)}&count=1";

            // Console.WriteLine("URL:" + url);

            var response = await httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode) {
                Console.WriteLine("It doesnt get anything from the URL!");
                return (null, null);
            }

            string content = await response.Content.ReadAsStringAsync();

            // Console.WriteLine("Content:\n" + content);

            var jsonDoc = JsonDocument.Parse(content);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("results", out JsonElement results) && results.GetArrayLength() > 0)
            {
                var firstResult = results[0];
                double lat = firstResult.GetProperty("latitude").GetDouble();
                double lon = firstResult.GetProperty("longitude").GetDouble();
                // Console.WriteLine("Latitude:" + lat + " , " + "Longitude" + lon);
                return (lat, lon);
            }
            return (null, null);
        }

        public async Task<List<WeatherData>> FetchWeatherDataAsync(string city)
        {
            var (latitude, longitude) = await GetCityCoordinatesAsync(city);
            
            if (latitude == null || longitude == null)
                return null;

            string url = $"https://api.open-meteo.com/v1/forecast" +
                $"?latitude={latitude}&longitude={longitude}" +
                $"&hourly=relative_humidity_2m,visibility,uv_index" +
                $"&timezone=UTC";

            var response = await httpClient.GetStringAsync(url);
            var jsonDoc = JsonDocument.Parse(response);
            var root = jsonDoc.RootElement;

            var hourly = root.GetProperty("hourly");
            var timeStamps = hourly.GetProperty("time").EnumerateArray().ToArray();
            var humidities = hourly.GetProperty("relative_humidity_2m").EnumerateArray().ToArray();
            var visibilities = hourly.TryGetProperty("visibility", out var vis) ? vis.EnumerateArray().ToArray() : null;
            var uvIndexes = hourly.TryGetProperty("uv_index", out var uv) ? uv.EnumerateArray().ToArray() : null;

            var weatherDataList = new List<WeatherData>();

            for (int i = 0; i < timeStamps.Length; i++)
            {
                DateTime timestamp = DateTime.Parse(timeStamps[i].GetString());
                double humidity = humidities[i].GetDouble();
                double visibility = visibilities?[i].GetDouble() ?? 0;
                double uvIndex = uvIndexes?[i].GetDouble() ?? 0;

                weatherDataList.Add(new WeatherData(humidity, visibility, uvIndex, timestamp));
            }

            return weatherDataList;
        }
    }
}