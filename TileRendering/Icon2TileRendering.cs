using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace TileRendering
{
    public class Icon2TileRendering : ObjectRendering
    {
        public Icon2TileRendering():base()
        {
           
        }

        public Icon2TileRendering(Stream stream)
        { 
         LoadFromStream( stream);
        }

        public byte[] Scale(double scale)
        {
            //_bitmap.SaveToFile(@"D:\Import\Icons\Notscaled.png", ImageFormat.Png);
            _bitmap = _bitmap.ResizeImage((int)(_bitmap.Width * scale), (int)(_bitmap.Height * scale));
            DetectFormat();
            //_bitmap.SaveToFile(@"D:\Import\Icons\Scaled.png", ImageFormat.Png);
            return GetBytes();
        }
     
        public void PasteTile(Stream stream)
        {
            Rectangle bounds = new Rectangle(0, 0, TILE_SIZE, TILE_SIZE);
            CopyRegionIntoImage(new Bitmap(stream), bounds, bounds);
        }
/// <summary>
/// Размещает иконку на тайле
/// </summary>
/// <param name="zoom"></param>
/// <param name="Lon"></param>
/// <param name="Lat"></param>
/// <param name="xTile"></param>
/// <param name="yTile"></param>
/// <param name="iconImage"></param>
        public void PasteImageToTileByLatLon(int zoom, double Lon, double Lat, int xTile, int yTile, Bitmap iconImage)
        {


            int width = iconImage.Width;
            int height = iconImage.Height;

            CopyRegionIntoImage(iconImage, new Rectangle(0, 0, width, height),  GetTargetBound(zoom, Lon, Lat, xTile, yTile, width, height));

        }


        public void PasteFromStreamImageToTileByXY( int X, int Y, Stream stream)
        {
            using (Bitmap iconImage = new Bitmap(stream, false))
            {
                DetectFormat();
                int width = iconImage.Width; int halfWidth = width >> 2;
                int height = iconImage.Height; int halfHeight = height >> 2;
                CopyRegionIntoImage(iconImage, new Rectangle(0, 0, width, height), new Rectangle(X - halfWidth, Y - halfHeight, X + halfWidth,Y + halfHeight));

            }
        }

        public void PasteFromStreamScaledImageToTile(int zoom, double Lon, double Lat, int xTile, int yTile, double scale, Stream stream)
        {
            using (Bitmap iconImage = new Bitmap(stream, false))
            {
                ImageFormat format = iconImage.GetImageFormat();
                //if (format == null) throw new Exception(string.Format("Формат не опознан, len {0}",stream.Length));
                //else throw new Exception(string.Format("Формат {1}, len {0}", stream.Length, format));               
                PasteScaledBitmapImageToTileByLatLon(zoom, Lon, Lat, xTile, yTile, scale, iconImage);

            }
        }

        public void PasteFromBinaryScaledImageToTile(int zoom, double Lon, double Lat, int xTile, int yTile, double scale, byte[] bytes)
        {
            using (Bitmap iconImage = new Bitmap(new MemoryStream(bytes), false))
            {
                PasteScaledBitmapImageToTileByLatLon(zoom, Lon, Lat, xTile, yTile, scale, iconImage);
            }
        }

        public void PasteScaledImageToTile(int zoom, double Lon, double Lat, int xTile, int yTile, double scale, Bitmap iconImage)
        {

            PasteScaledBitmapImageToTileByLatLon(zoom, Lon, Lat, xTile, yTile, scale, iconImage);


        }


        void PasteScaledBitmapImageToTileByLatLon(int zoom, double Lon, double Lat, int xTile, int yTile, double scale, Bitmap iconImage)
        {
            int width = iconImage.Width;
            int height = iconImage.Height;
            if (scale != 1.0)
            {
                width = (int)(width * scale);
                height = (int)(height * scale);

                
            }
            CopyRegionIntoImage(iconImage.ResizeImage((width), (height )), new Rectangle(0, 0, width, height), GetTargetBound(zoom, Lon, Lat, xTile, yTile, width, height));
            
        }

        public void PasteIconWithScale(byte[] bytes, int x, int y, double scale)
        {
            using (Bitmap icon = new Bitmap(new MemoryStream(bytes), false))
            {
                PasteIcon(icon, x, y, scale);
            }
        }

        public void PasteIconWithScale(Stream stream, int x, int y, double scale)
        {
            using (Bitmap icon = new Bitmap(stream, false))
            PasteIcon(icon, x, y, scale);
        }

        public void PasteIcon(string filename, int x, int y, double scale)
        { 
            using (Bitmap icon = new Bitmap(filename, false))
            PasteIcon( icon,  x,  y,  scale);

           
        }

        void PasteIcon(Bitmap icon, int x, int y, double scale)
        {
            int width = icon.Width;
            int height = icon.Height;
            if (scale != 1.0)
            {     
                icon.ResizeImage((int)(width * scale), (int)(height * scale));
                width = (int)(width * scale);
                height = (int)(height * scale);
            }

            CopyRegionIntoImage(icon, new Rectangle(0, 0, width, height),  new Rectangle(x, y, width, width));
            
        }

        #region [Pixel Position Calculation]
        Rectangle GetTargetBound(int zoom, double Lon, double Lat, int xTile, int yTile, int width, int height)
        {
            int xPix = _conv.FromLongitudeToXPixel(Lon, zoom);
            int yPix = _conv.FromLatitudeToYPixel(Lat, zoom);
            int xPos = xPix - xTile * TILE_SIZE;
            int yPos = yPix - yTile * TILE_SIZE;

            int halfWidth = width / 2;
            int halfHeight = height / 2;
            return new Rectangle(xPos - halfWidth, yPos - halfHeight, width, height);
        }

        int GetPixelXOnTile(int zoom, double Lon, int xTile)
        {
            return _conv.FromLongitudeToXPixel(Lon, zoom) - xTile * TILE_SIZE;
                   
        }
        int GetPixelYOnTile(int zoom, double Lat, int yTile)
        {
            
            return _conv.FromLatitudeToYPixel(Lat, zoom) - yTile * TILE_SIZE;
           
          

           
        }
        #endregion [Pixel Position Calculation]

        void CopyRegionIntoImage(Bitmap srcBitmap, Rectangle srcRegion, Rectangle destRegion)
        {
          
           
            _graphics.DrawImage(srcBitmap, destRegion, srcRegion, GraphicsUnit.Pixel);
            srcBitmap.Dispose();
           
        }

    
    }
}
