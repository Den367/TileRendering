using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Types;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace TileRendering
{
    public class ShapeToTileRendering : ObjectRendering
    {

        #region [Cutting]

        private SqlGeometry CutZoomedPixelMultiLineStringByTile(SqlGeometry poly, int X, int Y)
        {
            SqlGeometry result;
            SqlGeometry tile = _conv.GetTilePixelBound(X, Y, 1);
            result = poly.STIntersection(tile);
            return result;
        }


        /// <summary>
        /// Вовращает полигон и контур для указанного тайла для полигона без внутренних колец
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <returns></returns>
        private List<SqlGeometry> CutZoomedPixelPolygonByTile(SqlGeometry poly, int X, int Y)
        {
            List<SqlGeometry> result = new List<SqlGeometry>();
            SqlGeometry tile = _conv.GetTilePixelBound(X, Y, 1);
            var tiled = poly.STIntersection(tile);
            // Получаем контур полигона и внутренние кольца в виде MULTILINESTRING
            var contour = PolygonToMultiLineString(tiled);
            result.Add(tiled);
            // удаляем линии среза геометрии по границе тайла
            var tileLineString = tile.ToLineString();
            var tobecut = contour.STIntersection(tileLineString);
            var stroke = contour.STDifference(tobecut);
            result.Add(stroke);
            return result;
        }

        /// <summary>
        /// Cuts already pixeled geometry by the frame of a tile
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <returns></returns>
        private List<SqlGeometry> CutZoomedPixelPolygonByZeroTile(SqlGeometry poly)
        {
            List<SqlGeometry> result = new List<SqlGeometry>();
            SqlGeometry stroke = null;
            SqlGeometry contour;
            SqlGeometry tileLineString;
            SqlGeometry tobecut;
            SqlGeometry tile = _conv.GetTilePixelBound(0, 0, 1);
            var tiled = poly.STIntersection(tile);
            result.Add(tiled);
            switch (GetOpenGisGeometryType(tiled))
            {

                case OpenGisGeometryType.Polygon:
                    // Получаем контур полигона и внутренние кольца в виде MULTILINESTRING
                    contour = PolygonToMultiLineString(tiled);
                    // удаляем линии среза геометрии по границе тайла
                    tileLineString = tile.ToLineString();
                    tobecut = contour.STIntersection(tileLineString);
                    stroke = contour.STDifference(tobecut);
                    break;
                case OpenGisGeometryType.MultiPolygon:
                    // Получаем контур полигона и внутренние кольца в виде MULTILINESTRING
                    contour = MultiPolygonToMultiLineString(tiled);
                    // удаляем линии среза геометрии по границе тайла
                    tileLineString = tile.ToLineString();
                    tobecut = contour.STIntersection(tileLineString);
                    stroke = contour.STDifference(tobecut);
                    break;
            }
            result.Add(stroke);
            return result;
        }

        private List<SqlGeometry> CutPolygonByZoomedPixelTile(SqlGeometry poly, int X, int Y, int Z)
        {
            return CutZoomedPixelPolygonByTile(_parser.ConvertToZoomedPixelGeometry(poly, Z), X, Y);
        }

        /// <summary>
        /// Cuts shape by tile frame for specified zoom
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Z"></param>
        /// <returns></returns>
        private List<SqlGeometry> CutPolygonByZoomedPixelZeroTile(SqlGeometry poly, int X, int Y, int Z)
        {
            return CutZoomedPixelPolygonByZeroTile(_parser.ConvertToZoomedPixelZeroedByTileGeometry(poly, Z, X, Y));
        }


        private SqlGeometry MultiPolygonToMultiLineString(SqlGeometry multipoly)
        {
            SqlGeometry result;
            var geomBuilder = new SqlGeometryBuilder();
            geomBuilder.SetSrid((0));
            int geomnum = (int) multipoly.STNumGeometries();
            for (int g = 1; g <= geomnum; g++)
            {
                AddMultiLineStringFromPolygon(geomBuilder, multipoly.STGeometryN(g));
            }
            result = geomBuilder.ConstructedGeometry;
            if (result.STIsValid()) return result;
            else return result.MakeValid();
        }

        /// <summary>
        /// Формирует контур полигона с внутренними кольцами 
        /// </summary>
        /// <param name="poly">Полигон для извлечения контура</param>
        /// <returns>Возвращает MULTILINESTRING или LINESTRING при отсутствии внутренних колец</returns>
        private SqlGeometry PolygonToMultiLineString(SqlGeometry poly)
        {
            SqlGeometry result;
            int intnum = (int) poly.STNumInteriorRing();
            var exteriorRing = poly.STExteriorRing();
            var geomBuilder = new SqlGeometryBuilder();
            geomBuilder.SetSrid((0));
            AddMultiLineStringFromPolygon(geomBuilder, poly);
            result = geomBuilder.ConstructedGeometry;
            if (result.STIsValid()) return result;
            else return result.MakeValid();
        }

        private void AddMultiLineStringFromPolygon(SqlGeometryBuilder geomBuilder, SqlGeometry poly)
        {

            int intnum = (int) poly.STNumInteriorRing();
            var exteriorRing = poly.STExteriorRing();

            if (intnum > 0) geomBuilder.BeginGeometry(OpenGisGeometryType.MultiLineString);
            geomBuilder.BeginGeometry(OpenGisGeometryType.LineString);
            var startpoint = exteriorRing.STStartPoint();
            geomBuilder.BeginFigure((double) startpoint.STX, (double) startpoint.STY);
            for (int i = 2; i <= exteriorRing.STNumPoints(); i++)
                geomBuilder.AddLine((double) exteriorRing.STPointN(i).STX, (double) exteriorRing.STPointN(i).STY);
            geomBuilder.EndFigure();
            geomBuilder.EndGeometry();
            if (intnum > 0)
            {
                SqlGeometry intRing;
                SqlGeometry point;
                for (int i = 1; i <= intnum; i++)
                {
                    intRing = poly.STInteriorRingN(i);
                    geomBuilder.BeginGeometry(OpenGisGeometryType.LineString);
                    startpoint = intRing.STStartPoint();
                    geomBuilder.BeginFigure((double) startpoint.STX, (double) startpoint.STY);
                    for (int p = 2; p <= intRing.STNumPoints(); p++)
                    {
                        point = intRing.STPointN(p);
                        geomBuilder.AddLine((double) point.STX, (double) point.STY);
                    }
                    geomBuilder.EndFigure();
                    geomBuilder.EndGeometry();
                }

                geomBuilder.EndFigure();
                geomBuilder.EndGeometry();
            }
        }

        #endregion [Cutting]

        #region [ctor]

        public ShapeToTileRendering()
            : base()
        {
            _parser = new GeometryParser();
        }

        #endregion [ctor]


        /// <summary>
        /// Выполняет рендеринг части геометрии пересекающейся с тайлом на образе тайла
        /// </summary>
        /// <param name="shape">полная геометрия объекта в угловых координатах (широта, долгота)</param>
        /// <param name="X">номер тайла X</param>
        /// <param name="Y">номер тайла Y</param>
        /// <param name="Zoom">номер масштаба</param>
        /// <param name="argbFill">цвет в ARGB</param>
        /// <param name="argbStroke"></param>
        /// <param name="strokeWidth">ширина контура</param>
        public void DrawPartObjectShapeOnTile(SqlGeometry shape, int X, int Y, int Zoom, string argbFill,
                                              string argbStroke, int strokeWidth)
        {
            PasteShapeOnTile(CreateColor(argbFill), CreateColor(argbStroke), strokeWidth,
                             CutPolygonByZoomedPixelZeroTile(shape, X, Y, Zoom));
        }

        /// <summary>
        /// Pastes pexeled and ramed geometry on the zero tile
        /// </summary>
        /// <param name="fillcolor"></param>
        /// <param name="strokecolor"></param>
        /// <param name="width"></param>
        /// <param name="geom"></param>
        private void PasteShapeOnTile(Color fillcolor, Color strokecolor, int width, List<SqlGeometry> geom)
        {

            SqlGeometry shape = geom[0];
            int geomnum = (int) shape.STNumGeometries();

            SqlGeometry stroke = null;
            SqlGeometry ring;
            int intnum;
            if (geom != null)
                switch (GetOpenGisGeometryType(shape))
                {
                    case OpenGisGeometryType.LineString:
                    case OpenGisGeometryType.MultiLineString:
                        DrawMultiLineStringBordered2(shape, fillcolor, strokecolor, width, 1);
                        break;
                    case OpenGisGeometryType.Polygon:

                        intnum = (int) shape.STNumInteriorRing();
                        ring = shape.STExteriorRing();
                        // 1. рисуем полигон без внутренних колец
                        FillPolygonOnTile(fillcolor, ring.ToPointsArray());
                        // 2. рисуем внутренние кольца
                        if (geomnum >= 1) stroke = geom[1];
                        for (int i = 1; i <= intnum; i++)
                        {
                            FillTransparentPolygonOnTile(shape.STInteriorRingN(i).ToPointsArray());
                        }
                        // 3. рисуем контур
                        if (geom.Count > 1)
                        {
                            stroke = geom[1];
                            DrawContourOnTile(stroke, strokecolor, width);
                        }
                        break;
                    case OpenGisGeometryType.MultiPolygon:
                        break;
                }

        }

        #region [Aux Drawing]

        private void DrawContourOnTile(SqlGeometry stroke, Color strokecolor, int width)
        {

            switch (GetOpenGisGeometryType(stroke))
            {
                case OpenGisGeometryType.MultiLineString:
                    DrawMultiLineString(stroke, strokecolor, width);

                    break;
                case OpenGisGeometryType.LineString:
                    DrawStroke(strokecolor, width, stroke.ToPointsArray());
                    break;
            }
        }

        private void DrawMultiLineString(SqlGeometry stroke, Color strokecolor, int width)
        {

            using (Pen pen = new Pen(strokecolor, width))
            {
                int geomnum = (int) stroke.STNumGeometries();
                for (int i = 1; i <= geomnum; i++)
                {
                    _graphics.DrawLines(pen, stroke.STGeometryN(i).ToPointsArray());
                }
            }
        }

        private void DrawMultiLineStringBordered(SqlGeometry stroke, Color strokecolor, Color bordercolor, int coreWidth,
                                                 int borderWidth)
        {

            int geomnum = (int) stroke.STNumGeometries();
            for (int i = 1; i <= geomnum; i++)
            {
                DrawBorderedStroke(strokecolor, bordercolor, coreWidth, borderWidth,
                                   stroke.STGeometryN(i).ToPointsArray());
            }
        }

        private void DrawMultiLineStringBordered2(SqlGeometry stroke, Color strokecolor, Color bordercolor,
                                                  int coreWidth, int borderWidth)
        {
            int geomnum = (int) stroke.STNumGeometries();
            using (var pen = new Pen(Color.White))
            {
                for (int i = 1; i <= geomnum; i++)
                {
                    DrawBorderedStroke2(pen, strokecolor, bordercolor, coreWidth, borderWidth,
                                        stroke.STGeometryN(i).ToPointsArray());
                }
            }
        }

        #region [Drawing Polygon]

        private void DrawPolygonFillOnTile(SqlGeometry fill, string argbFill, string argbStroke, int zoom)
        {
            //int argb = Int32.Parse(argbFill, NumberStyles.HexNumber);
            //Color clr = Color.FromArgb(argb);
            using (Brush brush = new SolidBrush(CreateColor(argbFill)))
            {
                _graphics.FillPolygon(brush, ToPoint(fill, zoom));
            }

        }


        private void DrawPolygonStrokeOnTile(SqlGeometry fill, string argbFill, int thickness, int zoom)
        {

            //Color clr = Color.FromArgb(Int32.Parse(argbFill, NumberStyles.HexNumber));
            using (Pen brush = new Pen(CreateColor(argbFill), thickness))
            {
                _graphics.DrawLines(brush, ToPoint(fill, zoom));
            }
        }


        private void FillPolygonOnTile(Color color, Point[] points)
        {
            using (Brush brush = new SolidBrush(color))
            {
                _graphics.FillPolygon(brush, points);
            }
        }

        private void FillTransparentPolygonOnTile(Point[] points)
        {
            _graphics.FillPolygon(Brushes.Transparent, points);
        }

        #endregion [ Drawing Polygon]

        private void DrawStroke(Color color, int width, Point[] points)
        {
            using (var pen = new Pen(color, width))
            {
                _graphics.SmoothingMode = SmoothingMode.AntiAlias;
                _graphics.DrawLines(pen, points);
            }
        }


        private void DrawStroke(Pen pen, Point[] points)
        {

            //    _graphics.SmoothingMode = SmoothingMode.AntiAlias;
            _graphics.DrawLines(pen, points);

        }

        private void DrawStyledStroke(Color color, Color bordercolor, int corelinewidth, int borderwidth, Point[] points)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddLines(points);
                int width = borderwidth*2 + corelinewidth;
                using (Pen linePen = this.CreatePen(width, borderwidth, color))
                {

                    int bordersharewidth = borderwidth/width;
                    int coresharewidth = corelinewidth/width;
                    linePen.LineJoin = LineJoin.Round;
                    linePen.CompoundArray = new Single[]
                        {0.0F, bordersharewidth, bordersharewidth + coresharewidth, 1.0F};
                    _graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    _graphics.DrawPath(linePen, path);
                }
            }
        }

        private void DrawBorderedStroke(Color color, Color bordercolor, int coreWidth, int borderWidth, Point[] points)
        {
            DrawStroke(Color.White, borderWidth*2 + coreWidth + 2, points);
            DrawStroke(bordercolor, borderWidth*2 + coreWidth, points);
            DrawStroke(color, coreWidth, points);
        }

        private void DrawBorderedStroke2(Pen pen, Color color, Color bordercolor, int coreWidth, int borderWidth,
                                         Point[] points)
        {
            _graphics.SmoothingMode = SmoothingMode.AntiAlias;
            DrawStrokeByPen(pen, Color.White, borderWidth*2 + coreWidth + 2, points);
            DrawStrokeByPen(pen, bordercolor, borderWidth*2 + coreWidth, points);
            DrawStrokeByPen(pen, color, coreWidth, points);
        }

        private void DrawStrokeByPen(Pen pen, Color color, int width, Point[] points)
        {
            pen.Color = color;
            pen.Width = width;
            _graphics.DrawLines(pen, points);
        }

        #endregion [Aux Drawing]

        #region [Aux]

        private Point[] ToPoint(SqlGeometry geom, int zoom)
        {

            SqlGeometry fill;
            if (geom.STNumGeometries() > 1)
                fill = geom.STGeometryN(0);
            else fill = geom;
            int n = (int) fill.STNumPoints();
            Point[] points = new Point[n];
            for (int i = 0; i < n; i++)
            {
                points[i] = _conv.FromGeometryPointToPoint(fill.STPointN(i), zoom);
            }
            return points;
        }

        private Pen CreatePen(int width, int offset, Color color)
        {
            Pen pen = new Pen(color, width);
            if (offset == 0)
            {
                return pen;
            }
            else
            {
                return (this.ModifyPenWithOffset(pen, offset));
            }

        }

        private Pen ModifyPenWithOffset(Pen pen, int offset)
        {
            var originalPenWidth = pen.Width;
            pen.Width = 2*(pen.Width + Math.Abs(offset));
            if (offset > 0)
            {
                var calculation = originalPenWidth/pen.Width;
                pen.CompoundArray = new Single[] {0.0F, calculation};
                return pen;
            }
            else
            {
                var calculation = (pen.Width - originalPenWidth)/pen.Width;
                pen.CompoundArray = new Single[] {calculation, 1.0F};
                return pen;
            }

        }

        private Color CreateColor(string argb)
        {
            return Color.FromArgb(Int32.Parse(argb, NumberStyles.HexNumber));
        }

        private OpenGisGeometryType GetOpenGisGeometryType(SqlGeometry geom)
        {
            return (OpenGisGeometryType) Enum.Parse(typeof (OpenGisGeometryType), (string) geom.STGeometryType());
        }
    }

    #endregion [Aux]
}
