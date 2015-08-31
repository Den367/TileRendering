
//Для преобразования координат использован код google:
//http://code.google.com/p/geographical-dot-net/source/browse/trunk/GeographicalDotNet/GeographicalDotNet/Projection/GoogleMapsAPIProjection.cs
using System;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Microsoft.SqlServer.Types;

/// <summary>
/// Методы преобазования гео-координат в пиксели на тайле и обратно
/// реализованные в виде SQLCLR функций
/// Для использования в хранимых процедурах
/// </summary>
public class GoogleProjection
{
    private const double PixelTileSize = 256d;
    private const double DegreesToRadiansRatio = 180d/Math.PI;
    private const double RadiansToDegreesRatio = Math.PI/180d;


    [SqlFunction(IsDeterministic = true, DataAccess = DataAccessKind.None)]
    public static SqlInt64 FromLongitudeToXPixel(SqlDouble Longitude, SqlDouble zoomLevel)
    {
        var pixelGlobeSize = PixelTileSize*Math.Pow(2.0, (double) zoomLevel);
        var x = Math.Round((double) (Convert.ToSingle(pixelGlobeSize/2d) + (Longitude*(pixelGlobeSize/360d))));
        return Convert.ToInt64(x);
    }

    [SqlFunction(IsDeterministic = true, DataAccess = DataAccessKind.None)]
    public static SqlInt64 FromLatitudeToYPixel(SqlDouble Latitude, SqlDouble zoomLevel)
    {
        var pixelGlobeSize = PixelTileSize*Math.Pow(2.0, (double) zoomLevel);
        var f = Math.Min(Math.Max(Math.Sin((double) (Latitude*RadiansToDegreesRatio)), -0.9999d), 0.9999d);
        var y =
            Math.Round(Convert.ToSingle(pixelGlobeSize/2d) +
                       .5d*Math.Log((1d + f)/(1d - f))*-(pixelGlobeSize/(2d*Math.PI)));
        return Convert.ToInt64(y);
    }

    #region [Pixel to Coordinate]

    [SqlFunction(IsDeterministic = true, DataAccess = DataAccessKind.None)]
    public static SqlDouble FromXPixelToLongitude(SqlDouble pixelX, SqlDouble zoomLevel)
    {
        return FromXPixelToLon((long) pixelX, (double) zoomLevel);

    }

    private static double FromXPixelToLon(long pixelX, double zoomLevel)
    {
        var pixelGlobeSize = PixelTileSize*Math.Pow(2d, (double) zoomLevel);
        var XPixelsToDegreesRatio = pixelGlobeSize/360d;

        double halfPixelGlobeSize = Convert.ToSingle(pixelGlobeSize/2d);

        var longitude = (pixelX - halfPixelGlobeSize)/XPixelsToDegreesRatio;

        return Convert.ToDouble(longitude);
    }


    [SqlFunction(IsDeterministic = true, DataAccess = DataAccessKind.None)]
    public static SqlDouble FromYPixelToLatitude(SqlInt64 pixelY, SqlDouble zoomLevel)
    {
        return FromYPixelToLat((long) pixelY, (double) zoomLevel);
    }

    private static double FromYPixelToLat(long pixelY, double zoomLevel)
    {

        var pixelGlobeSize = PixelTileSize*Math.Pow(2d, zoomLevel);

        double YPixelsToRadiansRatio = pixelGlobeSize/(2d*Math.PI);
        double halfPixelGlobeSize = Convert.ToDouble(pixelGlobeSize/2d);

        var latitude = (2*Math.Atan(Math.Exp(((double) pixelY - halfPixelGlobeSize)/-YPixelsToRadiansRatio)) - Math.PI/2)*
                       DegreesToRadiansRatio;
        return Convert.ToDouble(latitude);
    }

    /// <summary>
    /// Возвращает геометрию в виде прямоугольника с долготой и широтой в координатах,
    /// Геометрия перекрывает иконку, что позволяет определить какие тайлы пересекаются с объектом.
    /// </summary>
    /// <param name="Longitude"></param>
    /// <param name="Latitude"></param>
    /// <param name="Width"></param>
    /// <param name="Height"></param>
    /// <param name="Zoom"></param>
    /// <param name="PixelYOffset"></param>
    /// <returns></returns>
    [SqlFunction(IsDeterministic = true, DataAccess = DataAccessKind.None)]
    public static SqlGeometry GetImageBound(SqlDouble Longitude, SqlDouble Latitude, SqlInt32 Width, SqlInt32 Height,
                                            SqlDouble Zoom, SqlInt32 PixelYOffset)
    {

        long cpX, cpY, LeftTopX, LeftTopY, RightBottomX, RightBottomY;
        long halfWidth = ((long) Width) >> 1;
        long halfHeight = ((long) Height) >> 1;
        double dZoom = (double) Zoom;
        // получить центральный пиксел по коорд
        cpX = (long) FromLongitudeToXPixel(Longitude, Zoom);
        cpY = (long) (FromLatitudeToYPixel(Latitude, Zoom) + PixelYOffset);
        LeftTopX = cpX - halfWidth;
        LeftTopY = cpY - halfHeight;
        RightBottomX = cpX + halfWidth;
        RightBottomY = cpY + halfHeight;
        double Lat1, Lon1, Lat2, Lon2;
        Lat1 = FromYPixelToLat(LeftTopY, dZoom);
        Lon1 = FromXPixelToLon(LeftTopX, dZoom);
        Lat2 = FromYPixelToLat(RightBottomY, dZoom);
        Lon2 = FromXPixelToLon(RightBottomX, dZoom);

        // 
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



    #endregion
}
