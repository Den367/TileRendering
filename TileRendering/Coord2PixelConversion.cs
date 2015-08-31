//Для преобразования координат использован код google:
//http://code.google.com/p/geographical-dot-net/source/browse/trunk/GeographicalDotNet/GeographicalDotNet/Projection/GoogleMapsAPIProjection.cs
using System;
using Microsoft.SqlServer.Types;
using System.Drawing;

namespace TileRendering
{
public   class Coord2PixelConversion
    {
        private const double PixelTileSize = 256d;
        private const double DegreesToRadiansRatio = 180d / Math.PI;
        private const double RadiansToDegreesRatio = Math.PI / 180d;

        #region [From coords to pixel]
        public  int FromLongitudeToXPixel(double longitude, double zoomLevel)
        {
            var pixelGlobeSize = PixelTileSize * Math.Pow(2.0, (double)zoomLevel);
            int x = (int)Math.Round((double)(Convert.ToSingle(pixelGlobeSize / 2d) + (longitude * (pixelGlobeSize / 360d))));
            return x;
        }

      
        public  int FromLatitudeToYPixel(double latitude, double zoomLevel)
        {
            var pixelGlobeSize = PixelTileSize * Math.Pow(2.0, (double)zoomLevel);
            var f = Math.Min(Math.Max(Math.Sin((double)(latitude * RadiansToDegreesRatio)), -0.9999d), 0.9999d);
            int y = (int)Math.Round(Convert.ToSingle(pixelGlobeSize / 2d) + .5d * Math.Log((1d + f) / (1d - f)) * -(pixelGlobeSize / (2d * Math.PI)));
            return y;
        }

        public Point GetPointFromCoords(double longitude, double latitude, double zoomLevel)
        {
            var pixelGlobeSize = PixelTileSize * Math.Pow(2.0, (double)zoomLevel);
            int x = (int)Math.Round((double)(Convert.ToSingle(pixelGlobeSize / 2d) + (longitude * (pixelGlobeSize / 360d))));
            var f = Math.Min(Math.Max(Math.Sin((double)(latitude * RadiansToDegreesRatio)), -0.9999d), 0.9999d);
            int y = (int) Math.Round(Convert.ToSingle(pixelGlobeSize / 2d) + .5d * Math.Log((1d + f) / (1d - f)) * -(pixelGlobeSize / (2d * Math.PI)));
            return new Point(x, y);
        }

        public PointF GetPointFromPixelPosition(long pixelX, long pixelY, int zoomLevel)
        {
             var pixelGlobeSize = PixelTileSize * Math.Pow(2d, (double)zoomLevel);
            var xPixelsToDegreesRatio = pixelGlobeSize / 360d;
            double halfPixelGlobeSize = Convert.ToDouble(pixelGlobeSize / 2d);
            var longitude = (pixelX - halfPixelGlobeSize) / xPixelsToDegreesRatio;
            double yPixelsToRadiansRatio = pixelGlobeSize / (2d * Math.PI);
            var latitude = (2 * Math.Atan(Math.Exp(((double)pixelY - halfPixelGlobeSize) / -yPixelsToRadiansRatio)) - Math.PI / 2) * DegreesToRadiansRatio;
            return new PointF(Convert.ToSingle(longitude),Convert.ToSingle(latitude));
        }


        #endregion [From coords to pixel]

        double FromXPixelToLon(long pixelX, double zoomLevel)
        {
            var pixelGlobeSize = PixelTileSize * Math.Pow(2d, (double)zoomLevel);
            var xPixelsToDegreesRatio = pixelGlobeSize / 360d;
            double halfPixelGlobeSize = Convert.ToSingle(pixelGlobeSize / 2d);
            var longitude = (pixelX - halfPixelGlobeSize) / xPixelsToDegreesRatio;
            return Convert.ToDouble(longitude);
        }

        double FromYPixelToLat(long pixelY, double zoomLevel)
        {
            var pixelGlobeSize = PixelTileSize * Math.Pow(2d, zoomLevel);
            double yPixelsToRadiansRatio = pixelGlobeSize / (2d * Math.PI);
            double halfPixelGlobeSize = Convert.ToDouble(pixelGlobeSize / 2d);
            var latitude = (2 * Math.Atan(Math.Exp(((double)pixelY - halfPixelGlobeSize) / -yPixelsToRadiansRatio)) - Math.PI / 2) * DegreesToRadiansRatio;
            return Convert.ToDouble(latitude);
        }


        public Point FromGeometryPointToPoint(SqlGeometry geom, int zoomLevel)
        {
            return GetPointFromCoords((double)geom.STX, (double)geom.STY, zoomLevel);
        }

        public SqlGeometry GetImageBound(double Longitude, double Latitude, int Width, int Height, double Zoom, int PixelYOffset)
        {
            long cpX, cpY, LeftTopX, LeftTopY, RightBottomX, RightBottomY;
            long halfWidth = ((long)Width) >> 1;
            long halfHeight = ((long)Height) >> 1;
            double dZoom = (double)Zoom;
            // получить центральный пиксел по коорд
            cpX = (long)FromLongitudeToXPixel(Longitude, Zoom);
            cpY = (long)(FromLatitudeToYPixel(Latitude, Zoom) + PixelYOffset);
            LeftTopX = cpX - halfWidth;
            LeftTopY = cpY - halfHeight;
            RightBottomX = cpX + halfWidth;
            RightBottomY = cpY + halfHeight;              
         return  GetGeoRectangle(FromXPixelToLon(LeftTopX, dZoom), FromYPixelToLat(LeftTopY, dZoom),  FromXPixelToLon(RightBottomX, dZoom), FromYPixelToLat(RightBottomY, dZoom));

        }

    /// <summary>
    /// Возвращает геометрию тайла с широтой и долготой в координатах
    /// </summary>
    /// <param name="tileX"></param>
    /// <param name="tileY"></param>
    /// <param name="Zoom"></param>
    /// <param name="Ext"></param>
    /// <returns></returns>
        public SqlGeometry GetTileBound(int tileX,int tileY, int Zoom, int Ext)
        {
            PointF LeftTop = GetPointFromPixelPosition(tileX * 256 - Ext, tileY  * 256 - Ext, Zoom);
            PointF RightBottom = GetPointFromPixelPosition((tileX + 1) * 256 + Ext, (tileY + 1) * 256 + Ext, Zoom);         
            return GetGeoRectangle(LeftTop, RightBottom);          
        }
    /// <summary>
    /// Возвращает геометрию тайла с номерами пикселей в координатах
    /// </summary>
    /// <param name="tileX"></param>
    /// <param name="tileY"></param>
    /// <param name="Ext"></param>
    /// <returns></returns>
        public SqlGeometry GetTilePixelBound(int tileX, int tileY, int Ext)
        {
            Point LeftTop = new Point(tileX * 256 - Ext, tileY * 256 - Ext);
            Point RightBottom = new Point((tileX + 1) * 256 + Ext, (tileY + 1) * 256 + Ext);
            return GetGeoRectangle(LeftTop, RightBottom);
        }

        SqlGeometry GetGeoRectangle(double Lon1, double Lat1, double Lon2, double Lat2)
        { 
          var geomBuilder = new SqlGeometryBuilder();
            geomBuilder.SetSrid((0));
            geomBuilder.BeginGeometry(OpenGisGeometryType.Polygon);
            geomBuilder.BeginFigure(Lon1, Lat1);
            geomBuilder.AddLine(Lon1, Lat2);
            geomBuilder.AddLine(Lon2, Lat2);
            geomBuilder.AddLine(Lon2, Lat1);
            geomBuilder.AddLine(Lon1, Lat1);
            geomBuilder.EndFigure();
            geomBuilder.EndGeometry();
            return geomBuilder.ConstructedGeometry;
        }

        SqlGeometry GetGeoRectangle(PointF topleft, PointF bottomright)
        { 
          var geomBuilder = new SqlGeometryBuilder();
            geomBuilder.SetSrid((0));
            geomBuilder.BeginGeometry(OpenGisGeometryType.Polygon);
            geomBuilder.BeginFigure(topleft.X, topleft.Y);
            geomBuilder.AddLine(topleft.X, bottomright.Y);
            geomBuilder.AddLine(bottomright.X, bottomright.Y);
            geomBuilder.AddLine(bottomright.X, topleft.X);
            geomBuilder.AddLine(topleft.X, topleft.Y);
            geomBuilder.EndFigure();
            geomBuilder.EndGeometry();
            return geomBuilder.ConstructedGeometry;
        }

    }
}
