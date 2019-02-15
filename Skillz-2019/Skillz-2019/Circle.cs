using System.Collections.Generic;
using ElfKingdom;

namespace MyBot
{
    public class Circle
    {
        public Location Center { get; }

        public int Radius { get; }

        public Circle(Location center, int radius)
        {
            Center = center;
            Radius = radius;
        }

        /// <summary>
        ///     returns all the locations that are in the map and on the circle
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Location> GetCircleLocations()
        {
            var locations = new HashSet<Location>();
            for (int i = 0; i <= Radius; i++)
            {
                int x1 = Center.Col + i, x2 = Center.Col - i;
                var y1 = GetY(x1);
                var y2 = GetY(x2);
                Location l1 = new Location((int)y1.Item1, x1), l2 = new Location((int)y1.Item2, x1);
                Location l3 = new Location((int)y2.Item1, x2), l4 = new Location((int)y2.Item2, x2);

                foreach (Location location in new [] {l1, l2, l3, l4})
                {
                    if (location.InMap())
                        locations.Add(location);
                }
            }

            return locations;
        }

        /// <summary>
        ///     returns the Y values for the given X value
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private System.Tuple<double, double> GetY(int x)
        {
            int a = Center.Col, b = Center.Row;
            double sqrt = (Radius - (x - a)) * (Radius + (x - a));
            sqrt = System.Math.Sqrt(sqrt);
            return System.Tuple.Create(b + sqrt, b - sqrt);
        }
    }
}
