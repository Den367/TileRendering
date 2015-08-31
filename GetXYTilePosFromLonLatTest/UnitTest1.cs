using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TileCopy.Bounding;
using System.Diagnostics;

namespace GetXYTilePosFromLonLatTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestFromLatLonToPix()
        { 
            TilePosition position1;
            TilePosition position2;

            BoundCalculator bound = new BoundCalculator(new BoundingFrame());
            Trace.WriteLine(string.Format("Левая верхняя точка: {0} {1} Правая нижняя точка {2} {3}", 29.315801, 59.385591, 31.309820, 60.254130));
            for (int zoom = 0; zoom <= 17; zoom++)
            {
                position1 = bound.FromLatLonToPix(29.315801, 59.385591, zoom);
                position2 = bound.FromLatLonToPix(31.309820, 60.254130, zoom);
                Trace.WriteLine(string.Format("Левый верхний тайл: {0} {1} Правый нижний тайл: {2} {3}", position1.X, position1.Y, position2.X, position2.Y));
               
            }
        }
    }
}
