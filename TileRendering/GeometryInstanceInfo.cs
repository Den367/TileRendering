using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Microsoft.SqlServer.Types;

namespace TileRendering
{
    public class GeometryInstanceInfo
    {
        /// <summary>
        /// Тип геометрии
        /// </summary>
        public OpenGisGeometryType ShapeType { get; set; }
        // Наборы точек геометрических фигур (линии, внешние и внутренние кольца полигонов)
        public List<List<GeometryPointSequence>> Points { get; set; }
        public GeometryInstanceInfo[] GeometryInstanceInfoCollection { get; set; }
        
    }
}
