using System;
using System.Reactive.Subjects;

namespace reactive_web_server.Reactive
{
    public static class LocationStream
    {
        private static readonly Subject<string> locationSubject = new Subject<string>();

        public static IObservable<string> LocationObservable => locationSubject;

        public static void PushLocation(string location)
        {
            Console.WriteLine($"[PushLocation] Received location: {location}");

            if (!string.IsNullOrWhiteSpace(location))
            {
                locationSubject.OnNext(location.Trim());
            }
        }
    }
}
