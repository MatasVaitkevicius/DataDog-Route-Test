using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Datadog_Route
{
    class Program
    {
        static void Main(string[] args)
        {
            var chosenLatitude = 0.0;
            var chosenLongitude = 0.0;
            try
            {
                Console.WriteLine("Enter latitude: ");
                if (!double.TryParse(Console.ReadLine(), NumberStyles.Any, CultureInfo.InvariantCulture, out chosenLatitude))
                {
                    throw new FormatException("Latitude must be in correct format");
                }
                
                Console.WriteLine("Enter Longitude: ");
                if (!double.TryParse(Console.ReadLine(), NumberStyles.Any, CultureInfo.InvariantCulture, out chosenLongitude))
                {
                    throw new FormatException("Longitude must be in correct format");
                }
                const double fuel = 2000;
                var breweryGeocodesData = "geocodes.csv";
                var beerData = "beers.csv";
                var breweryData = "breweries.csv";
                var beers = ReadBeerData(beerData);
                var breweryGeocodes = ReadBreweryGeocodes(breweryGeocodesData);
                var breweries = ReadBreweryData(breweryData);
                var timer = System.Diagnostics.Stopwatch.StartNew();
                var shortestPath = ShortestPath(breweryGeocodes, chosenLatitude, chosenLongitude, fuel);
                var elapsed = timer.ElapsedMilliseconds;

                PrintResult(shortestPath, beers, breweries, elapsed);
                Console.ReadKey();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.ReadKey();
        }

        public static void PrintResult(List<BreweryGeocodes> shortestPath, Dictionary<string, List<Beer>> beers, Dictionary<string, Brewery> breweries, long elapsedTime)
        {
            var beerTypes = new List<Beer>();
            var breweriesNames = new List<Brewery>();
            var wholeFlight = shortestPath.Sum(s => s.Distance);

            Console.WriteLine();
            Console.WriteLine($"Found {shortestPath.Count() - 2} beer factories");
            Console.WriteLine();
            shortestPath.ForEach(s =>
            {
                var brewery = breweries.ContainsKey(s.BreweryId) ? breweries[s.BreweryId] : null;

                Console.WriteLine($"[{s.BreweryId}]: {brewery?.Name}  {s.Latitude}, {s.Longitude} distance {s.Distance}km");
                if (beers.ContainsKey(s.BreweryId))
                {
                    beerTypes.AddRange(beers[s.BreweryId]);
                }

            });
            Console.WriteLine();
            Console.WriteLine($"Total distance travelled {wholeFlight}km");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine($"Collected {beerTypes.Count()} beer types:");
            beerTypes.ForEach(b => Console.WriteLine($"     ->{b.Name}"));
            Console.WriteLine();
            Console.WriteLine($"Program took: {elapsedTime}ms");
        }

        public static List<BreweryGeocodes> ReadBreweryGeocodes(string breweryGeocodesData)
        {
            var breweryGeocodes = new List<BreweryGeocodes>();
            using (var reader = new StreamReader(breweryGeocodesData))
            {
                var line = reader.ReadLine();
                line = reader.ReadLine();
                while (line != null)
                {
                    var values = line.Split(',');
                    var id = values[0];
                    var breweryId = values[1];
                    var lat = double.Parse(values[2], CultureInfo.InvariantCulture);
                    var lon = double.Parse(values[3], CultureInfo.InvariantCulture);
                    var accuracy = values[4];

                    breweryGeocodes.Add(new BreweryGeocodes
                    {
                        Id = id,
                        BreweryId = breweryId,
                        Latitude = lat,
                        Longitude = lon,
                        Accuracy = accuracy,
                    });
                    line = reader.ReadLine();
                }
            }
            return breweryGeocodes;
        }

        public static Dictionary<string, List<Beer>> ReadBeerData(string beerData)
        {
            var beers = new Dictionary<string, List<Beer>>();
            using (var reader = new StreamReader(beerData))
            {
                var line = reader.ReadLine();
                line = reader.ReadLine();
                while (line != null)
                {
                    var values = line.Split(',');
                    if (int.TryParse(values[0], out int n) && int.TryParse(values[1], out int m))
                    {
                        var id = values[0];
                        var breweryId = values[1];
                        var name = values[2];
                        var beer = new Beer
                        {
                            Id = id,
                            BreweryId = breweryId,
                            Name = name
                        };
                        if (beers.ContainsKey(breweryId))
                        {
                            beers[breweryId].Add(beer);
                        }
                        else
                        {
                            beers.Add(breweryId, new List<Beer>() { beer });
                        }
                    }
                    line = reader.ReadLine();
                }
            }
            return beers;
        }

        public static Dictionary<string, Brewery> ReadBreweryData(string breweryData)
        {
            var breweries = new Dictionary<string, Brewery>();
            using (var reader = new StreamReader(breweryData))
            {
                var line = reader.ReadLine();
                line = reader.ReadLine();
                while (line != null)
                {
                    var values = line.Split(',');
                    if (int.TryParse(values[0], out int n))
                    {
                        var breweryId = values[0];
                        var name = values[1];
                        var brewery = new Brewery
                        {
                            BreweryId = breweryId,
                            Name = name
                        };
                        breweries.Add(breweryId, brewery);
                    }
                    line = reader.ReadLine();
                }
            }
            return breweries;
        }

        public static List<BreweryGeocodes> SortBreweryDistances(List<BreweryGeocodes> breweryGeocodes, BreweryGeocodes brewery)
        {
            breweryGeocodes.ForEach(b =>
            {
                var distance = DistanceBetweenBreweries(brewery, b);
                b.Distance = distance;
            });

            return breweryGeocodes.OrderBy(s => s.Distance).ToList();
        }

        static List<BreweryGeocodes> ShortestPath(List<BreweryGeocodes> breweryGeocodes, double chosenLatitude, double chosenLongitude, double fuel)
        {
            var shortestPath = new List<BreweryGeocodes>();
            var home = new BreweryGeocodes
            {
                Latitude = chosenLatitude,
                Longitude = chosenLongitude,
                BreweryId = "Home",
                Distance = 0
            };
            shortestPath.Add(home);
            var distanceToHome = 0.0;

            while (fuel >= distanceToHome)
            {
                var lastBrewery = shortestPath.LastOrDefault();
                breweryGeocodes = SortBreweryDistances(breweryGeocodes, lastBrewery);
                var brewery = breweryGeocodes.FirstOrDefault();
                distanceToHome = DistanceBetweenBreweries(home, brewery);
                fuel -= brewery.Distance;
                if (fuel >= distanceToHome)
                {
                    shortestPath.Add(brewery);
                }
                breweryGeocodes.RemoveAt(0);
            }

            var flightHome = new BreweryGeocodes
            {
                Latitude = chosenLatitude,
                Longitude = chosenLongitude,
                BreweryId = "Flight Home",
                Distance = DistanceBetweenBreweries(home, shortestPath.LastOrDefault())
            };
            shortestPath.Add(flightHome);

            return shortestPath;
        }

        public static double DistanceBetweenBreweries(BreweryGeocodes breweryFrom, BreweryGeocodes breweryTo)
        {
            double rlat1 = Math.PI * breweryFrom.Latitude / 180;
            double rlat2 = Math.PI * breweryTo.Latitude / 180;
            double theta = breweryFrom.Longitude - breweryTo.Longitude;
            double rtheta = Math.PI * theta / 180;
            double dist = Math.Sin(rlat1) * Math.Sin(rlat2) + Math.Cos(rlat1) * Math.Cos(rlat2) * Math.Cos(rtheta);
            dist = Math.Acos(dist);
            dist = dist * 180 / Math.PI;
            dist = dist * 60 * 1.1515;
    
            return dist * 1.609344;
        }
    }
}

