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
using System.IO;
using nQuant;
using TileRendering;
using System.Drawing;
using System.Drawing.Imaging;

public partial class BitmapFunctions
{
    [Microsoft.SqlServer.Server.SqlFunction]
    public static SqlBinary IconTile(SqlBinary image, SqlInt32 zoom, SqlDouble Lon, SqlDouble Lat, SqlInt32 xTile, SqlInt32 yTile, SqlDouble scale)
    {
        SqlBinary result = null;
        using (Icon2TileRendering paster = new Icon2TileRendering())
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(image.Value, 0, image.Length);
                SetBeginPosition(ms);
                paster.PasteFromStreamScaledImageToTile((int)zoom, (double)Lon, (double)Lat, (int)xTile, (int)yTile, (double)scale, ms);
                result = paster.GetBytes();
            }
        }
        return result;
    }

    [SqlFunction]
    public static SqlBinary ShapeTile(SqlGeometry shape, SqlInt32 zoom,  SqlInt32 xTile, SqlInt32 yTile, SqlString argbFill,SqlString argbStroke,SqlInt32 strokeWidth)
    {
        SqlBinary result = null;
        using (ShapeToTileRendering paster = new ShapeToTileRendering())
        {
            using (MemoryStream ms = new MemoryStream())
            {
                try
                {
                    paster.DrawPartObjectShapeOnTile(shape, (int) xTile, (int) yTile, (int) zoom, argbFill.ToString(),
                                                     argbStroke.ToString(), (int) strokeWidth);
                    result = paster.GetBytes();
                }
                catch (System.Exception ex)
                {
                    //string innerMessage = ex.InnerException.Message;
                    //throw new Exception(string.Format("zoom: {1}; X:{2}; Y:{3} {0} , inner: {4}", shape, zoom, xTile,yTile, innerMessage));
                }
                return result;
            }
        }
        
      
    }
    

  

    [SqlFunction]
    public static SqlBinary ScaleImage(SqlBinary image, SqlDouble scale)
    {
        SqlBinary result = null;
        using (MemoryStream ms = new MemoryStream())
        {
            ms.Write(image.Value, 0, image.Length);
           SetBeginPosition(ms);

            using (Icon2TileRendering bitmap = new Icon2TileRendering(ms))
            {
                result = bitmap.Scale((double)scale);
            }
        }
        // Put your code here
        return result;
    }

    [SqlFunction]
    public static SqlBoolean SaveToFile(SqlBinary image, SqlString filePath, SqlString mimeType)
    {
        SqlBoolean result = false;
        using (MemoryStream ms = new MemoryStream())
        {
            ms.Write(image.Value, 0, image.Length);
            SetBeginPosition(ms);

            using (Icon2TileRendering bitmap = new Icon2TileRendering(ms))
            {
                 bitmap.SaveToFile((string)filePath,(string)mimeType);
                 result = true;
            }
        }
        // Put your code here
        return result;
    }

    [SqlFunction]
    public static SqlBoolean SaveToFolderByZoomXY(SqlBinary image, SqlString rootFolderPath, SqlInt32 Zoom, SqlInt32 X, SqlInt32 Y)
    {
        SqlBoolean result = false;
        using (MemoryStream ms = new MemoryStream())
        {
            ms.Write(image.Value, 0, image.Length);
            SetBeginPosition(ms);

            using (Icon2TileRendering bitmap = new Icon2TileRendering(ms))
            {
                bitmap.SaveToPngFile((string)rootFolderPath, Zoom.ToString(), X.ToString(), Y.ToString());
                result = true;
            }
        }
        // Put your code here
        return result;
    }


      [SqlFunction]
    public static SqlBoolean SaveToFolderByZoomXY8bpp(SqlBinary image, SqlString rootFolderPath, SqlInt32 Zoom, SqlInt32 X, SqlInt32 Y)
    {
        SqlBoolean result = false;
          using (MemoryStream ms = new MemoryStream())
          {
              ms.Write(image.Value, 0, image.Length);
              SetBeginPosition(ms);

              using (var bitmap = new Bitmap(ms))
              {
                  if (bitmap == null) throw new Exception("Не удалось инициализировать объект Bitmap из Stream");
                  var quantizer = new WuQuantizer();
                  if (quantizer == null) throw new Exception("Не удалось инициализировать объект WuQuantizer");
                  using (var quantized = quantizer.QuantizeImage(bitmap))
                  {
                      if (quantized == null) throw new Exception("Не удалось инициализировать объект Image с помощью экземпляра WuQuantizer");
                      //try
                      //{
                      string zoomFolder = string.Format(@"{0}/{1}", rootFolderPath, Zoom);
                      CheckFolderExistsCreate(zoomFolder);
                      string xFolder = string.Format(@"{0}/{1}", zoomFolder, X);
                      CheckFolderExistsCreate(xFolder);
                      quantized.Save(string.Format("{0}/{1}/{2}/{3}.png", rootFolderPath, Zoom, X, Y), ImageFormat.Png);

                      //using (MemoryStream msquant = new MemoryStream())
                      //{
                      //    quantized.Save(msquant, ImageFormat.Png);
                      //    SetBeginPosition(msquant);
                      //    using (Icon2TileRendering renderer = new Icon2TileRendering(msquant))
                      //    {
                      //        renderer.SaveToPngFile((string) rootFolderPath, Zoom.ToString(), X.ToString(), Y.ToString());
                      //        result = true;
                      //    }
                      //}
                      //Image i2 = new Bitmap(quantized);
                      //CheckFolder((string)rootFolderPath, Zoom.ToString(), X.ToString(), Y.ToString())
                      //;
                      //i2.Save(string.Format("{0}/{1}/{2}/{3}.png", rootFolderPath, Zoom, X, Y), ImageFormat.Png);

                          //var img = ImageToStreamToImage(quantized);
                          //img.Save(string.Format("{0}/{1}/{2}/{3}.png", rootFolderPath, Zoom, X, Y), ImageFormat.Png);
                          //quantized.Save(string.Format("{0}/{1}/{2}/{3}.png", rootFolderPath, Zoom, X, Y), ImageFormat.Png);
                      //}
                      //catch (System.Exception ex)
                      //{
                      //    throw new Exception(string.Format("SaveToFolderByZoomXY8bpp {0}", ex.Message));
                      //}

                  }
              }
          }
          // Put your code here
        return result;
    }

      static void CheckFolderExistsCreate(string folderPath)
      {
          if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
      }

    private static Image ImageToStreamToImage(Image bmp)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            bmp.Save(ms, ImageFormat.Png);
            SetBeginPosition(ms);
            return Image.FromStream(ms);
            
        }
    }



    static void SetBeginPosition(Stream stream)
    {
      
        stream.Seek(0, SeekOrigin.Begin);
        stream.Position = 0;
    }
}
