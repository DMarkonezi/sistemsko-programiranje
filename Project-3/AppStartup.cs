using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using reactive_web_server.Reactive;
using reactive_web_server.Weather;
using reactive_web_server.Stats;

namespace reactive_web_server
{
    public static class AppStartup
    {
        public static WeatherStatsService WeatherStatsService { get; private set; }

        public static void Initialize()
        {
            var weatherService = new WeatherService();
            WeatherStatsService = new WeatherStatsService();

            LocationStream.LocationObservable
                .ObserveOn(TaskPoolScheduler.Default)
                .Select(city =>
                    Observable.FromAsync(() =>
                        weatherService.FetchWeatherDataAsync(city)
                    )
                )
                .Switch()
                .Subscribe(data =>
                {
                    if (data != null)
                    {
                        WeatherStatsService.UpdateWeatherStats(data);
                        Console.WriteLine($"[Thread {Environment.CurrentManagedThreadId}] Updated stats from weather API");
                    }
                });
        }
    }
}
