using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Types;
using System.Drawing;

namespace TileRendering
{
    public static class GeometryExtension
    {

        public static SqlGeometry ToLineString(this SqlGeometry poly)
        {

            var geomBuilder = new SqlGeometryBuilder();
            geomBuilder.SetSrid((0));
            geomBuilder.BeginGeometry(OpenGisGeometryType.LineString);
            var startpoint = poly.STStartPoint();
            geomBuilder.BeginFigure((double)startpoint.STX, (double)startpoint.STY);
            for (int i = 1; i <= poly.STNumPoints(); i++)
            {
                geomBuilder.AddLine((double)poly.STPointN(i).STX, (double)poly.STPointN(i).STY);
            }
            geomBuilder.EndFigure();
            geomBuilder.EndGeometry();
            return geomBuilder.ConstructedGeometry;
        }

        public static IEnumerable<PointF> ToPointsF(this SqlGeometry geom)
        {

            SqlGeometry fill;
            if (geom.STNumGeometries() > 1)
                fill = geom.STGeometryN(1);
            else fill = geom;
            int n = (int)fill.STNumPoints();
            List<PointF> points = new List<PointF>();
            for (int i = 1; i <= n; i++)
            {
                points.Add(new PointF((float)fill.STPointN(i).STX, (float)fill.STPointN(i).STY));
            }
            return points;
        }

        /// <summary>
        /// Возвращает последовательность точек первой геометрии
        /// </summary>
        /// <param name="geom"></param>
        /// <returns></returns>
        public static PointF[] ToPointsFArray(this SqlGeometry geom)
        {

            SqlGeometry fill;
            if (geom.STNumGeometries() > 1) fill = geom.STGeometryN(1);
            else fill = geom;
            int n = (int)fill.STNumPoints();
            PointF[] points = new PointF[n];
            for (int i = 1; i <= n; i++)
            {
                points[i - 1] = new PointF((float)fill.STPointN(i).STX, (float)fill.STPointN(i).STY);
            }
            return points;
        }

        public static Point[] ToPointsArray(this SqlGeometry geom)
        {

            SqlGeometry fill;
            if (geom.STNumGeometries() > 1)  fill = geom.STGeometryN(1);
            else fill = geom;
            int n = (int)fill.STNumPoints();
            Point[] points = new Point[n];
            for (int i = 1; i <= n; i++)
            {
                points[i - 1] = new Point((int)fill.STPointN(i).STX, (int)fill.STPointN(i).STY);
            }
            return points;
        }

        public static IEnumerable<PointF> ToPointsOfGeometryN(this SqlGeometry geom, int N)
        {

            SqlGeometry fill;
            int num = (int)geom.STNumGeometries();
            if (num >= 1 && num > N)
                fill = geom.STGeometryN(N);
            else fill = geom;
            return fill.ToPointsF();
        }

        public static PointF[] ToPointsArrayOfGeometryN(this SqlGeometry geom, int N)
        {

            SqlGeometry fill;
            int num = (int)geom.STNumGeometries();
            if (num >= 1 && num > N)    fill = geom.STGeometryN(N);
            else fill = geom;
            return fill.ToPointsFArray();
        }

        public static List<List<GeometryPointSequence>> ToGeometryPointsOfPolygon(this SqlGeometry geom)
        {
            List<List<GeometryPointSequence>> pointsList = new List<List<GeometryPointSequence>>();
            pointsList.Add(GetPolygonPointSequence(geom));
            return pointsList;
        }

      

        public static List<List<GeometryPointSequence>> ToGeometryPointsOfMultiPolygon(this SqlGeometry geom)
        {
            List<List<GeometryPointSequence>> pointsList = new List<List<GeometryPointSequence>>();
            int numGeometries = (int)geom.STNumGeometries();
            List<GeometryPointSequence> points = new List<GeometryPointSequence>();

            for (int i = 1; i <= numGeometries; i++)                            
                pointsList.Add(GetPolygonPointSequence(geom.STGeometryN(i)));                   
            return pointsList;
        }

        public static List<List<GeometryPointSequence>> ToGeometryPointsOfMultiPointLineString(this SqlGeometry geom)
        {
            List<List<GeometryPointSequence>> pointsList = new List<List<GeometryPointSequence>>();
            int numGeometries = (int)geom.STNumGeometries();
            List<GeometryPointSequence> points = new List<GeometryPointSequence>();

            for (int i = 1; i <= numGeometries; i++)
            {
                points.Clear();
                points.Add(new GeometryPointSequence() { PointList = geom.STGeometryN(i).ToPointsFArray(), InnerRing = false });
                pointsList.Add(points);
            }
            return pointsList;
        }


        static List<GeometryPointSequence> GetPolygonPointSequence(SqlGeometry geom)
        {
            List<GeometryPointSequence> pointsList = new List<GeometryPointSequence>();
            int numInteriorRing = (int)geom.STNumInteriorRing();
            GeometryPointSequence points = new GeometryPointSequence();
            points.PointList = geom.STExteriorRing().ToPointsFArray();
            points.InnerRing = false;
            pointsList.Add(points);
            if (numInteriorRing > 1)

                for (int i = 1; i <= numInteriorRing; i++)
                {
                    points.PointList = geom.STInteriorRingN(i).ToPointsFArray();
                    points.InnerRing = true;
                    pointsList.Add(points);
                }


            return pointsList;
        }

        public static OpenGisGeometryType GetGeometryType(this SqlGeometry geom)
        {
            return (OpenGisGeometryType)Enum.Parse(typeof(OpenGisGeometryType), (string)geom.STGeometryType());

        }
       
    }
}
