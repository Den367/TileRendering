using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Microsoft.SqlServer.Types;

namespace TileRendering
{
    public class GeometryParser
    {
        //GeometryInstanceInfo _info;
        private const int TILE_SIZE = 256;

        #region [ctor]

        public GeometryParser()
        {
            _conv = new Coord2PixelConversion();
        }

        #endregion [ctor]

        private readonly Coord2PixelConversion _conv;

        public SqlGeometry ConvertToZoomedPixelGeometry(SqlGeometry shape, int zoom)
        {
            return CreateGeometryFromZoomedPixelInfo(ConvertToGeometryZoomedPixelsInfo(GetGeometryInfo(shape), zoom));
        }

        /// <summary>
        ///     Возврашает пиксельную геометрию смещённую относительно указанного тайла
        ///     для дальнейшей отрисовки 
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="zoom"></param>
        /// <param name="tileX"></param>
        /// <param name="tileY"></param>
        /// <returns></returns>
        public SqlGeometry ConvertToZoomedPixelZeroedByTileGeometry(SqlGeometry shape, int zoom, int tileX, int tileY)
        {
            return
                CreateGeometryFromZoomedPixelInfo(ConvertToGeometryZoomedPixelsZeroTileShiftedInfo(
                    GetGeometryInfo(shape), zoom, tileX, tileY));
        }

        public GeometryZoomedPixelsInfo ConvertToZoomedPixelsInfo(SqlGeometry shape, int zoom)
        {
            return ConvertToGeometryZoomedPixelsInfo(GetGeometryInfo(shape), zoom);
        }

        /// <summary>
        ///     Преобразует в списки координат с инфрмацией о типе геометрии
        /// </summary>
        /// <param name="shape"></param>
        /// <returns></returns>
        private GeometryInstanceInfo GetGeometryInfo(SqlGeometry shape)
        {
            var result = new GeometryInstanceInfo();
            var type = (OpenGisGeometryType) Enum.Parse(typeof (OpenGisGeometryType), (string) shape.STGeometryType());
            result.ShapeType = type;
            //List<PointF> points = new List<PointF>();
            PointF[] points;
            List<GeometryPointSequence> pointSequenceList;
            var shapesPointsCollection = new List<List<GeometryPointSequence>>();
            switch (type)
            {
                case OpenGisGeometryType.Point:
                    points = new PointF[1];
                    SqlGeometry firstpoint = shape.STStartPoint();
                    points[0] = new PointF((float) firstpoint.STX, (float) firstpoint.STY);
                    pointSequenceList = new List<GeometryPointSequence>
                        {
                            new GeometryPointSequence {PointList = points, InnerRing = false}
                        };
                    shapesPointsCollection.Add(pointSequenceList);
                    break;

                case OpenGisGeometryType.LineString:
                    points = shape.ToPointsFArray();
                    pointSequenceList = new List<GeometryPointSequence> { new GeometryPointSequence { PointList = points, InnerRing = false } };
                  
                    shapesPointsCollection.Add(pointSequenceList);
                    break;

                case OpenGisGeometryType.Polygon:
                    shapesPointsCollection = shape.ToGeometryPointsOfPolygon();
                    break;

                case OpenGisGeometryType.MultiPoint:
                case OpenGisGeometryType.MultiLineString:
                    shapesPointsCollection = shape.ToGeometryPointsOfMultiPointLineString();
                    break;

                case OpenGisGeometryType.MultiPolygon:
                    shapesPointsCollection = shape.ToGeometryPointsOfMultiPolygon();
                    break;

                case OpenGisGeometryType.GeometryCollection:
                    var geomNum = (int) shape.STNumGeometries();
                    var geometryList = new GeometryInstanceInfo[geomNum];
                    int i;
                    for ( i = 0; i < geomNum; i++)
                        geometryList[i] = GetGeometryInfo(shape.STGeometryN(i));
                    result.GeometryInstanceInfoCollection = geometryList;
                    break;
            }
            result.Points = shapesPointsCollection;
            return result;
        }

        private GeometryZoomedPixelsInfo ConvertToGeometryZoomedPixelsInfo(GeometryInstanceInfo info, int zoom)
        {
            var result = new GeometryZoomedPixelsInfo();
            var pixelCoordsListList = new List<List<GeometryPixelCoords>>();
            var geomPixCoordsList = new List<GeometryPixelCoords>();
            var coords = new GeometryPixelCoords {InnerRing = false};
            OpenGisGeometryType type = info.ShapeType;
            result.ShapeType = type;
            switch (type)
            {
                case OpenGisGeometryType.Point:
                    PointF[] geopoints = info.Points[0][0].PointList;

                    coords.PixelCoordList = new[]
                        {
                            new Point
                                {
                                    X = _conv.FromLongitudeToXPixel(geopoints[0].X, zoom),
                                    Y = _conv.FromLatitudeToYPixel(geopoints[0].Y, zoom)
                                }
                        };
                    geomPixCoordsList.Add(coords);
                    pixelCoordsListList.Add(geomPixCoordsList);

                    break;

                case OpenGisGeometryType.LineString:

                    coords.PixelCoordList = GetPixelCoords(info.Points[0][0].PointList, zoom);
                    geomPixCoordsList.Add(coords);
                    pixelCoordsListList.Add(geomPixCoordsList);
                    break;
                case OpenGisGeometryType.Polygon:
                    foreach (var list in info.Points)
                        foreach (GeometryPointSequence pointseq in list)
                        {
                            coords.PixelCoordList = GetPixelCoords(pointseq.PointList, zoom);
                            coords.InnerRing = pointseq.InnerRing;
                            geomPixCoordsList.Add(coords);
                        }
                    pixelCoordsListList.Add(geomPixCoordsList);

                    break;
                case OpenGisGeometryType.MultiPoint:
                case OpenGisGeometryType.MultiLineString:
                case OpenGisGeometryType.MultiPolygon:

                    pixelCoordsListList = GetGeometryPixelcoords(info.Points, zoom);
                    break;

                case OpenGisGeometryType.GeometryCollection:
                    GeometryInstanceInfo[] geomColl = info.GeometryInstanceInfoCollection;
                    int n = info.GeometryInstanceInfoCollection.Length;
                    var geomPixZoomInfoCollection = new GeometryZoomedPixelsInfo[n];
                    for (int i = 0; i < n; i++)
                    {
                        var geom = new GeometryZoomedPixelsInfo
                            {
                                ShapeType = geomColl[i].ShapeType,
                                Points = GetGeometryPixelcoords(geomColl[i].Points, zoom)
                            };

                        geomPixZoomInfoCollection[i] = geom;
                    }
                    result.GeometryInstanceInfoCollection = geomPixZoomInfoCollection;
                    break;
            }
            if (type != OpenGisGeometryType.GeometryCollection) result.Points = pixelCoordsListList;
            return result;
        }

        /// <summary>
        /// Converts to degree coordinates to pixels
        /// Shifts to tile with position numbers equals to zero
        /// </summary>
        /// <param name="info"></param>
        /// <param name="zoom"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private GeometryZoomedPixelsInfo ConvertToGeometryZoomedPixelsZeroTileShiftedInfo(GeometryInstanceInfo info,
                                                                                          int zoom, int x, int y)
        {
            int tilezeroshiftX = x*TILE_SIZE;
            int tilezeroshiftY = y*TILE_SIZE;
            var result = new GeometryZoomedPixelsInfo();
            var pixelCoordsListList = new List<List<GeometryPixelCoords>>();
            var geomPixCoordsList = new List<GeometryPixelCoords>();
            var coords = new GeometryPixelCoords {InnerRing = false};
            OpenGisGeometryType type = info.ShapeType;
            result.ShapeType = type;
            switch (type)
            {
                case OpenGisGeometryType.Point:
                    PointF[] geopoints = info.Points[0][0].PointList;

                    coords.PixelCoordList = new[]
                        {
                            new Point
                                {
                                    X = _conv.FromLongitudeToXPixel(geopoints[0].X, zoom) - tilezeroshiftX,
                                    Y = _conv.FromLatitudeToYPixel(geopoints[0].Y, zoom) - tilezeroshiftY
                                }
                        };
                    geomPixCoordsList.Add(coords);
                    pixelCoordsListList.Add(geomPixCoordsList);

                    break;

                case OpenGisGeometryType.LineString:

                    coords.PixelCoordList = GetPixelCoordsShifted(info.Points[0][0].PointList, zoom, tilezeroshiftX,
                                                                  tilezeroshiftY);
                    geomPixCoordsList.Add(coords);
                    pixelCoordsListList.Add(geomPixCoordsList);
                    break;
                case OpenGisGeometryType.Polygon:
                    foreach (var list in info.Points)
                        foreach (GeometryPointSequence pointseq in list)
                        {
                            coords.PixelCoordList = GetPixelCoordsShifted(pointseq.PointList, zoom, tilezeroshiftX,
                                                                          tilezeroshiftY);
                            coords.InnerRing = pointseq.InnerRing;
                            geomPixCoordsList.Add(coords);
                        }
                    pixelCoordsListList.Add(geomPixCoordsList);

                    break;
                case OpenGisGeometryType.MultiPoint:
                case OpenGisGeometryType.MultiLineString:
                case OpenGisGeometryType.MultiPolygon:

                    pixelCoordsListList = GetGeometryPixelCoordsShifted(info.Points, zoom, tilezeroshiftX,
                                                                        tilezeroshiftY);
                    break;

                case OpenGisGeometryType.GeometryCollection:
                    GeometryInstanceInfo[] geomColl = info.GeometryInstanceInfoCollection;
                    int n = info.GeometryInstanceInfoCollection.Length;
                    var geomPixZoomInfoCollection = new GeometryZoomedPixelsInfo[n];
                    for (int i = 0; i < n; i++)
                    {
                        var geom = new GeometryZoomedPixelsInfo();
                        geom.ShapeType = geomColl[i].ShapeType;
                        geom.Points = GetGeometryPixelCoordsShifted(geomColl[i].Points, zoom, tilezeroshiftX,
                                                                    tilezeroshiftY);

                        geomPixZoomInfoCollection[i] = geom;
                    }
                    result.GeometryInstanceInfoCollection = geomPixZoomInfoCollection;
                    break;
            }
            if (type != OpenGisGeometryType.GeometryCollection) result.Points = pixelCoordsListList;
            return result;
        }

        /// <summary>
        ///     Фомирует <see cref="SqlGeometry" /> по <see cref="GeometryZoomedPixelsInfo" />
        /// </summary>
        /// <param name="pixelData"></param>
        /// <returns></returns>
        private SqlGeometry CreateGeometryFromZoomedPixelInfo(GeometryZoomedPixelsInfo pixelData)
        {
            Point point;
            int geomnum, c, i;
            //Point[] points;
            var builder = new SqlGeometryBuilder();
            builder.SetSrid(0);
            OpenGisGeometryType type = pixelData.ShapeType;
            switch (type)
            {
                case OpenGisGeometryType.Point:
                    builder.BeginGeometry(OpenGisGeometryType.Point);
                    point = pixelData.Points[0][0].PixelCoordList[0];
                    builder.BeginFigure(point.X, point.Y);
                    builder.EndFigure();
                    builder.EndGeometry();
                    break;
                case OpenGisGeometryType.MultiPoint:
                    builder.BeginGeometry(OpenGisGeometryType.MultiPoint);
                    geomnum = pixelData.Points.Count;
                    for (i = 0; i < geomnum; i++)
                    {
                        builder.BeginGeometry(OpenGisGeometryType.Point);
                        point = pixelData.Points[i][0].PixelCoordList[0];
                        builder.BeginFigure(point.X, point.Y);
                        builder.EndFigure();
                        builder.EndGeometry();
                    }
                    builder.EndGeometry();
                    break;
                case OpenGisGeometryType.LineString:
                    builder.BeginGeometry(OpenGisGeometryType.LineString);
                    AddFigurePoints(builder, pixelData.Points[0][0].PixelCoordList);
                    builder.EndGeometry();
                    break;

                case OpenGisGeometryType.MultiLineString:
                    geomnum = pixelData.Points.Count;
                    builder.BeginGeometry(OpenGisGeometryType.MultiLineString);
                    for (c = 0; c < geomnum; c++)
                    {
                        builder.BeginGeometry(OpenGisGeometryType.LineString);
                        AddFigurePoints(builder, pixelData.Points[c][0].PixelCoordList);
                        builder.EndGeometry();
                    }
                    builder.EndGeometry();
                    break;
                case OpenGisGeometryType.Polygon:

                    builder.BeginGeometry(OpenGisGeometryType.Polygon);
                    AddRings(builder, pixelData.Points[0]);
                    builder.EndGeometry();
                    break;
                case OpenGisGeometryType.MultiPolygon:
                    geomnum = pixelData.Points.Count;
                    builder.BeginGeometry(OpenGisGeometryType.MultiPolygon);
                    for (i = 0; i < geomnum; i++)
                    {
                        builder.BeginGeometry(OpenGisGeometryType.Polygon);                        
                        AddRings(builder, pixelData.Points[i]);                       
                        builder.EndGeometry();
                    }
                    builder.EndGeometry();
                    break;
                case OpenGisGeometryType.GeometryCollection:
                    break;
            }
            SqlGeometry result = builder.ConstructedGeometry;
            if (result.STIsValid()) return result;
            else return result.MakeValid();
        }

        private static void AddFigurePoints(SqlGeometryBuilder builder, Point[] points)
        {
            int geomlen = points.Length;

            Point point = points[0];
            builder.BeginFigure(point.X, point.Y);
            for (int i = 1; i < geomlen; i++)
            {
                point = points[i];
                builder.AddLine(point.X, point.Y);
            }
            builder.EndFigure();
        }

        private static void AddFigureRingPoints(SqlGeometryBuilder builder, Point[] points)
        {
            try
            {
  int pointqty = points.Length;
                int unicqty = points.Distinct().Count();
                if (unicqty  < 3) return;
             if (points[0] != points[pointqty - 1]) return;
             if (points[0] != points[pointqty - 1]) return;

            Point point = points[0];
                Point prevPoint = point; 
            builder.BeginFigure(point.X, point.Y);
            for (int i = 1; i < pointqty; i++)
            {
                point = points[i];
                if (point != prevPoint) builder.AddLine(point.X, point.Y);
                prevPoint = point;
            }
            builder.EndFigure();
            }
            catch (Exception)
            {
                


                throw;
            }
          
        }

        private static void AddRings(SqlGeometryBuilder builder, List<GeometryPixelCoords> ring)
        {
            var circnum = ring.Count;
          
            for (int c = 0; c < circnum; c++)
                AddFigureRingPoints(builder, ring[c].PixelCoordList);
        }

        private List<List<GeometryPixelCoords>> GetGeometryPixelcoords(
            List<List<GeometryPointSequence>> geomInstanceGeoPoints, int zoom)
        {
            var pixelCoordsListList = new List<List<GeometryPixelCoords>>();
            var geomPixCoordsList = new List<GeometryPixelCoords>();
            var coords = new GeometryPixelCoords();
            foreach (var list in geomInstanceGeoPoints)
                foreach (GeometryPointSequence pointseq in list)
                {
                    coords.PixelCoordList = GetPixelCoords(pointseq.PointList, zoom);
                    coords.InnerRing = pointseq.InnerRing;
                    geomPixCoordsList.Add(coords);
                }
            pixelCoordsListList.Add(geomPixCoordsList);
            return pixelCoordsListList;
        }

        private List<List<GeometryPixelCoords>> GetGeometryPixelCoordsShifted(
            List<List<GeometryPointSequence>> geomInstanceGeoPoints, int zoom, int shiftedX, int shiftedY)
        {
            var pixelCoordsListList = new List<List<GeometryPixelCoords>>();
            var geomPixCoordsList = new List<GeometryPixelCoords>();
            var coords = new GeometryPixelCoords();
            foreach (var list in geomInstanceGeoPoints)
                foreach (GeometryPointSequence pointseq in list)
                {
                    coords.PixelCoordList = GetPixelCoordsShifted(pointseq.PointList, zoom, shiftedX, shiftedY);
                    coords.InnerRing = pointseq.InnerRing;
                    geomPixCoordsList.Add(coords);
                }
            pixelCoordsListList.Add(geomPixCoordsList);
            return pixelCoordsListList;
        }

        private Point[] GetPixelCoords(PointF[] geopoints, int zoom)
        {
            int n = geopoints.Length;
            var pixelpoints = new Point[n];
            for (int i = 0; i < n; i++)
                pixelpoints[i] = new Point
                    {
                        X = _conv.FromLongitudeToXPixel(geopoints[0].X, zoom),
                        Y = _conv.FromLatitudeToYPixel(geopoints[0].Y, zoom)
                    };
            return pixelpoints;
        }

        private Point[] GetPixelCoordsShifted(PointF[] geopoints, int zoom, int shiftX, int shiftY)
        {
            int n = geopoints.Length;
            var pixelpoints = new Point[n];
            for (int i = 0; i < n; i++)
                pixelpoints[i] = new Point
                    {
                        X = _conv.FromLongitudeToXPixel(geopoints[i].X, zoom) - shiftX,
                        Y = _conv.FromLatitudeToYPixel(geopoints[i].Y, zoom) - shiftY
                    };
            return pixelpoints;
        }
    }
}