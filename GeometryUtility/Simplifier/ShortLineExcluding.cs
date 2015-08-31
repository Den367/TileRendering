//------------------------------------------------------------------------------
// <copyright file="CSSqlFunction.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Data;
using System.Data.SqlClient;


using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Microsoft.SqlServer.Types;

namespace Simplifier
{


    /// <summary>
    /// Excluding short lines from geometries
    /// </summary>
    public partial class ShortLineExcluding
    {
        private const int MinPointCountToProceed = 10;
        private const int MinPointCountInPolygon = 4;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="shortestDistance"></param>
        /// <returns></returns>
        [Microsoft.SqlServer.Server.SqlFunction]
        public static SqlGeometry ExcludeShortLine(SqlGeometry shape, SqlDouble shortestDistance)
        {

            // Begin the geometry
            SqlGeometryBuilder _builder = new SqlGeometryBuilder();
            _builder.SetSrid(0);
            if (shape == null) return null;

            switch (GetOpenGisGeometryType(shape))
            {
                case OpenGisGeometryType.GeometryCollection:
                    return CreateMultiPolygonFromGeometryCollectionWithExcludingLineString(shape);
                case OpenGisGeometryType.LineString:
                case OpenGisGeometryType.MultiLineString:
                    // _builder.Dispose(); //EndGeometry();
                    return shape;
                    
                case OpenGisGeometryType.MultiPolygon:
                   
                    BuildMultipolygonSimplified(_builder, shape, shortestDistance);
                    break;
                case OpenGisGeometryType.Polygon:


                   
                    BuildPolygonSimplified(_builder, shape, shortestDistance);
                    break;
                default:
                    return shape;
                    
            }
           

            // Return the constructed geometry
            var resultGeometry = _builder.ConstructedGeometry;
            if (resultGeometry.STIsValid()) return resultGeometry;
            else
            {
               
                return CreateMultiPolygonFromGeometryCollectionWithExcludingLineString(resultGeometry.MakeValid());
            }
        }

        /// <summary>
        /// Builds multipoligon by each polygon inside geometry collection
        /// </summary>
        public static void BuildMultipolygonSimplified(SqlGeometryBuilder builder, SqlGeometry shape,
                                                      SqlDouble shortestDistance)
        {
            builder.BeginGeometry(OpenGisGeometryType.MultiPolygon);
            var polygonCount = shape.STNumGeometries();
            for (int i = 1; i <= polygonCount; i++)
            {
                BuildPolygonSimplified(builder, shape.STGeometryN(i), shortestDistance);
            }
            // End the geometry
            builder.EndGeometry();
        }

        private static void BuildPolygonSimplified(SqlGeometryBuilder builder, SqlGeometry shape,
                                                   SqlDouble shortestDistance)
        {
            try
            {
                builder.BeginGeometry(OpenGisGeometryType.Polygon);
                var internalRingsNum = (int) shape.STNumInteriorRing();
                var exteriorRing = shape.STExteriorRing();
                               
             // Proceed shortening external ring
                AddShortenedRingByBuilder(builder, exteriorRing, shortestDistance);
                // Proceed interior rings
                if (internalRingsNum > 0)
                for (int i = 1; i <= internalRingsNum; i++)
                {
                  AddShortenedRingByBuilder(builder, shape.STInteriorRingN(i), shortestDistance);
                }
                builder.EndGeometry();
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(" BuildPolygonSimplified: {0}", ex.Message));
            }


        }

        /// <summary>
        /// Add points to single ring in polygon
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="ring"></param>
        /// <param name="shortestDistance"></param>
        private static void AddShortenedRingByBuilder(SqlGeometryBuilder builder, SqlGeometry ring,
                                                      SqlDouble shortestDistance)
        {
            var pointCount = ring.STNumPoints();
            //var area = GetGeography(ring).STArea();
            if (pointCount <= MinPointCountToProceed ) AddRingByBuilder(builder, ring);
            else
            {
                try
                {
                    var firstPoint = ring.STPointN(1);
                    var startPoint = firstPoint;
                    int resulPointCount = 0;
                    // Begin the polygon with the first point

                    builder.BeginFigure((double) firstPoint.STX, (double) firstPoint.STY);
                    // While there are still unchecked points in polygon
                    for (int i = 2; i <= pointCount; i++)
                    {
                       
                        var secondPoint = ring.STPointN(i);
                        if (GetDistanceBetweenPoints(firstPoint, secondPoint) >= shortestDistance || (resulPointCount < MinPointCountInPolygon && i >= (pointCount - 1)))
                        {
                            resulPointCount++;
                            builder.AddLine((double) secondPoint.STX, (double) secondPoint.STY);
                            firstPoint = ring.STPointN(i);
                        }
                       
                    }
                    // Add last point - the same as first
                    builder.AddLine((double) startPoint.STX, (double) startPoint.STY);
                    builder.EndFigure();
                }
                catch (System.Exception ex)
                {

                    throw new Exception(string.Format(" AddShortenedRingByBuilder: {0}  Точек в кольце:{1}", ex.Message,
                                                      pointCount));
                }
            }
        }

        private static SqlGeography GetGeography(SqlGeometry ring)
        {
            if (ring.STIsValid()) ring = ring.MakeValid();
            return SqlGeography.STGeomFromWKB(ring.STUnion(ring.STStartPoint()).STAsBinary(), 4326);

        }

        private static void AddRingByBuilder(SqlGeometryBuilder builder, SqlGeometry ring)
        {
            try
            {

            
            var firstPoint = ring.STPointN(1);
            var startPoint = firstPoint;
            var pointCount = ring.STNumPoints();
            // Begin the polygon with the first point
            builder.BeginFigure((double) firstPoint.STX, (double) firstPoint.STY);
            // While there are still unchecked points in polygon
            for (int i = 2; i <= pointCount; i++)
            {
                var secondPoint = ring.STPointN(i);

                builder.AddLine((double) secondPoint.STX, (double) secondPoint.STY);


            }
            // Add last point - the same as first
            builder.AddLine((double) startPoint.STX, (double) startPoint.STY);
            builder.EndFigure();
            }
            catch (System.Exception ex)
            {

                throw new Exception(string.Format("  AddRingByBuilder: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Returns length of segment in polygon
        /// </summary>
        /// <param name="geometry"></param>
        /// <param name="firstPoint"></param>
        /// <param name="secondPoint"></param>
        /// <returns></returns>
        public static SqlDouble GetSegmentLength(SqlGeometry geometry, SqlInt32 firstPoint, SqlInt32 secondPoint)
        {
            SqlInt32 pointNums = geometry.STNumPoints();
            if (pointNums < firstPoint || pointNums < secondPoint) return 0;

            var firstCoords = geometry.STPointN((int) firstPoint);
            var secondCoords = geometry.STPointN((int) secondPoint);

            return GetDistanceBetweenPoints(firstCoords, secondCoords);

        }

        private static SqlGeometry CreateMultiPolygonFromGeometryCollectionWithExcludingLineString( SqlGeometry geometry)
        {
            SqlGeometryBuilder builder = new SqlGeometryBuilder();
            builder.SetSrid(0);
            if (GetOpenGisGeometryType(geometry) != OpenGisGeometryType.GeometryCollection) return geometry;
            builder.BeginGeometry(OpenGisGeometryType.MultiPolygon);
            var polygonCount = geometry.STNumGeometries();
            for (int i = 1; i <= polygonCount; i++)
            {
                var shape = geometry.STGeometryN(i);
                if (GetOpenGisGeometryType(shape) == OpenGisGeometryType.Polygon)
                {
                    RepeatPolygon(builder, shape);
                }
            }
            // End the geometry
            builder.EndGeometry();
            return builder.ConstructedGeometry;
        }

        private static void RepeatPolygon(SqlGeometryBuilder builder, SqlGeometry shape)
        {
            try
            {
                builder.BeginGeometry(OpenGisGeometryType.Polygon);
                var internalRingsNum = (int)shape.STNumInteriorRing();
                var exteriorRing = shape.STExteriorRing();

                // Proceed shortening external ring
                AddRingByBuilder(builder, exteriorRing);
                // Proceed interior rings
                if (internalRingsNum > 0)
                    for (int i = 1; i <= internalRingsNum; i++)
                    {
                        AddRingByBuilder(builder, shape.STInteriorRingN(i));
                    }
                builder.EndGeometry();
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(" RepeatPolygon: {0}", ex.Message));
            }
            
        }

        #region [Auxilary]
        /// <summary>
        /// Returns distance between two points 
        /// </summary>
        /// <param name="firstCoords">Geometry of first point  <see cref= "SqlGeometry"></see></param>
        /// <param name="secondCoords">Geometry of second point <see ref="SqlGeometry"></see></param>
        /// <returns></returns>
        public static SqlDouble GetDistanceBetweenPoints(SqlGeometry firstCoords, SqlGeometry secondCoords)
        {
            double firstLatitude = (double) firstCoords.STY*Math.PI/180.0;
            double firstLongitude = (double) firstCoords.STX*Math.PI/180.0;
            double secondLatitude = (double) secondCoords.STY*Math.PI/180.0;
            double secondLongitude = (double) secondCoords.STX*Math.PI/180.0;

            var latitudeDelta = secondLatitude - firstLatitude;
            var longitudeDelta = secondLongitude - firstLongitude;

            return (double) (12734*
                             Math.Asin(
                                 Math.Sqrt(Math.Sin(latitudeDelta/2)*Math.Sin(latitudeDelta/2) +
                                           Math.Cos(firstLatitude)*Math.Cos(secondLatitude)*Math.Sin(longitudeDelta/2)*
                                           Math.Sin(longitudeDelta/2))));
        }

        #endregion [Auxilary]
        private static OpenGisGeometryType GetOpenGisGeometryType(SqlGeometry geom)
        {
            return (OpenGisGeometryType) Enum.Parse(typeof (OpenGisGeometryType), (string) geom.STGeometryType());

        }

    }

}
