using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Microsoft.SqlServer.Types;

namespace PST.GeoSpatial.Drawing
{
    public partial class GeoSpatialBuilder
    {



        [Microsoft.SqlServer.Server.SqlFunction]
        public static SqlGeometry DrawGeoSpatialSector(SqlDouble longitude, SqlDouble latitude, SqlDouble azimuth,
                                                       SqlDouble angle, SqlDouble Radius)
        {
            return DrawGeoSpatialSectorVarAngle(longitude, latitude, azimuth, angle, Radius, 12.0);
        }

        /// <summary>
        /// Формирует геометрию сектора
        /// </summary>
        /// <param name="longitude"></param>
        /// <param name="latitude"></param>
        /// <param name="azimuth"></param>
        /// <param name="angle"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        [Microsoft.SqlServer.Server.SqlFunction]
        public static SqlGeometry DrawGeoSpatialSectorVarAngle(SqlDouble longitude, SqlDouble latitude, SqlDouble azimuth,
                                                       SqlDouble angle, SqlDouble radius, SqlDouble stepAngle)
        {

            if (longitude == SqlDouble.Null || latitude == SqlDouble.Null || azimuth == SqlDouble.Null ||
                angle == SqlDouble.Null || radius == SqlDouble.Null || radius == 0 || angle == 0)
                return SqlGeometry.Parse("GEOMETRYCOLLECTION EMPTY");           
            SqlGeometryBuilder builder = new SqlGeometryBuilder();
            builder.SetSrid(0);
            builder.BeginGeometry(OpenGisGeometryType.Polygon);           
            double firstPointLon;
            double firstPointLat;
            double sectorStepAngle = (double) stepAngle;
            const double earthRadius = 6367.0;
            double lat = (double) latitude;
            double lon = (double) longitude;
            double azim = (double) azimuth;
            double ang = (double) angle;
            double piRad = (Math.PI/180.0);
            double tLat = piRad*lat;
            double tLon = piRad*lon;
            double distkm = ((double) radius/1000)/earthRadius;
            double angleStart = azim - ang/2;
            double angleEnd = azim + ang/2;

            var _angle = Math.Abs(ang);
            if (_angle > 360.0)
            {
                angle = 360.0;
            }
            int pointCount = (int) Math.Floor(ang/sectorStepAngle);
            double brng;
            double latRadians;
            double lngRadians;
            double ptX;
            double ptY;
            int i = 0;

            if (angle < 360.0)
            {

                builder.BeginFigure(lon, lat);
                firstPointLon = lon;
                firstPointLat = lat;
            }
            else
            {

                brng = piRad*(angleStart);
                latRadians = Math.Asin(Math.Sin(tLat)*Math.Cos(distkm) + Math.Cos(tLat)*Math.Sin(distkm)*Math.Cos(brng));
                lngRadians = tLon +
                             Math.Atan2(Math.Sin(brng)*Math.Sin(distkm)*Math.Cos(tLat),
                                        Math.Cos(distkm) - Math.Sin(tLat)*Math.Sin(latRadians));
                ptX = 180.0*lngRadians/Math.PI;
                ptY = 180.0*latRadians/Math.PI;
                builder.BeginFigure(ptX, ptY);
                firstPointLon = ptX;
                firstPointLat = ptY;
            }
            while (i <= pointCount)
            {

                brng = piRad*(angleStart + i*sectorStepAngle);
                latRadians = Math.Asin(Math.Sin(tLat)*Math.Cos(distkm) + Math.Cos(tLat)*Math.Sin(distkm)*Math.Cos(brng));
                lngRadians = tLon +
                             Math.Atan2(Math.Sin(brng)*Math.Sin(distkm)*Math.Cos(tLat),
                                        Math.Cos(distkm) - Math.Sin(tLat)*Math.Sin(latRadians));
                ptX = 180.0*lngRadians/Math.PI;
                ptY = 180.0*latRadians/Math.PI;

                builder.AddLine(ptX, ptY);

                i = i + 1;
            }
            if (((angleStart + pointCount * sectorStepAngle) < angleEnd))
            {
                brng = piRad * (angleEnd);
                latRadians = Math.Asin(Math.Sin(tLat) * Math.Cos(distkm) + Math.Cos(tLat) * Math.Sin(distkm) * Math.Cos(brng));
                lngRadians = tLon +
                             Math.Atan2(Math.Sin(brng) * Math.Sin(distkm) * Math.Cos(tLat),
                                        Math.Cos(distkm) - Math.Sin(tLat) * Math.Sin(latRadians));
                ptX = 180.0 * lngRadians / Math.PI;
                ptY = 180.0 * latRadians / Math.PI;
                builder.AddLine(ptX, ptY);

            }
            builder.AddLine(firstPointLon, firstPointLat);
            builder.EndFigure();
            builder.EndGeometry();
            return builder.ConstructedGeometry;
        }
    }
}