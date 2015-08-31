using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Types;

namespace TileRendering
{
    public class GeometryZoomedPixelsInfo
    {
        /// <summary>
        /// Тип геометрии
        /// </summary>
        public OpenGisGeometryType ShapeType { get; set; }
        // Наборы точек геометрических фигур (линии, внешние и внутренние кольца полигонов)
        public List<List<GeometryPixelCoords>> Points { get; set; }
        public GeometryZoomedPixelsInfo[] GeometryInstanceInfoCollection { get; set; }
    }
}
