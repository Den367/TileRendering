using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace TileRendering
{
    public abstract class ObjectRendering: IDisposable
    {
        protected Bitmap _bitmap;
        protected ImageFormat _format;
        protected Graphics _graphics;
        protected Coord2PixelConversion _conv;
        protected GeometryParser _parser;
        protected const int TILE_SIZE = 256;

        public ObjectRendering()
        {
            _bitmap = GetInitialTile();
            _conv = new Coord2PixelConversion();
           
        }

        Bitmap GetInitialTile()
        {

            Bitmap DrawArea = new Bitmap(TILE_SIZE, TILE_SIZE);

            using (Graphics xGraph = Graphics.FromImage(DrawArea))
            {

                xGraph.FillRectangle(Brushes.Transparent, 0, 0, TILE_SIZE, TILE_SIZE);

                _graphics = Graphics.FromImage(DrawArea);

                return DrawArea;
            }
        }

        #region [Loading]
        public void Read(BinaryReader reader)
        {

            _bitmap = new Bitmap(new MemoryStream(reader.ReadBytes((int)reader.BaseStream.Length)));
            DetectFormat();
        }

        public void LoadFromStream(Stream stream)
        {
            _bitmap = new Bitmap(stream, false);
            DetectFormat();
        }

        public void LoadFromFile(string filename)
        {
            _bitmap = new Bitmap(filename, false);
            DetectFormat();
        }
        #endregion [Loading]
        #region [Folder]
        void CheckFolderExistsCreate(string folderPath)
        {
            if (!Directory.Exists(folderPath))Directory.CreateDirectory(folderPath);            
        }
        #endregion [Folder]
        #region [Saving]


        public void SaveToPngFile(string rootFolder, string zoom, string x, string y)
        {
            string zoomFolder = string.Format(@"{0}/{1}", rootFolder, zoom);
            CheckFolderExistsCreate(zoomFolder);
            string xFolder = string.Format(@"{0}/{1}", zoomFolder, x);
            CheckFolderExistsCreate(xFolder);
            _bitmap.Save(string.Format(@"{0}/{1}.png", xFolder, y), ImageFormat.Png);
        }

        public void SaveToPngFile(string filename)
        {

            _bitmap.Save(filename, ImageFormat.Png);
        }

        public void SaveToFile(string filename, string mimetype)
        {
            _bitmap.Save(filename, GetImageFormatFromMimeType(mimetype));
        }

        public void SaveToFile(string filename, ImageFormat format)
        {
            Image img = _bitmap.Convert();
            img.GetImageFormat();
            EncoderParameters encodeParams = new EncoderParameters(1);
            encodeParams.Param[0] = new EncoderParameter(Encoder.ColorDepth, 8L);
            img.Save(filename, GetEncoderInfo("image/png"), encodeParams);
            using (var stream = new FileStream(filename, FileMode.Create))
            {
               // _bitmap.Save(writer.BaseStream, _codecInfo, encodeParams);

                img.Save(stream, GetEncoderInfo("image/png"), encodeParams);
            }
        }

        public void Write(BinaryWriter writer)
        {
            _bitmap.Save(writer.BaseStream, _format);
        }

        public byte[] GetBytes()
        {
            return _bitmap.ToByteArray(ImageFormat.Png);
        }
        #endregion [Saving]

        #region [Streaming]
        public Stream GetBitmapStream(ImageFormat format)
        {
            MemoryStream memoryStream = new MemoryStream();
            _bitmap.Save(memoryStream, format);
            return memoryStream;

        }

      
        #endregion [Streaming]

        protected void DetectFormat()
        {
            _format = _bitmap.GetImageFormat();
        }


        static ImageFormat GetImageFormatFromMimeType(string mimetype)
        {

            switch (mimetype.ToLower())
            {
                case "image/png": return ImageFormat.Png;
                case "image/exif":
                case "image/jpeg": return ImageFormat.Jpeg;
                case "image/bmp": return ImageFormat.Bmp;
                case "image/x-emf": return ImageFormat.Emf;
                case "image/x-wmf": return ImageFormat.Wmf;


                case "image/gif": return ImageFormat.Gif;
                case "image/x-icon": return ImageFormat.Icon;
                case "image/tiff": return ImageFormat.Tiff;
                default: return ImageFormat.Png;
            }

        }

        ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            //do a case insensitive search for the mime type
            string lookupKey = mimeType.ToLower();

            //the codec to return, default to null
            ImageCodecInfo foundCodec = null;
            Dictionary<string, ImageCodecInfo> encoders = Encoders();
            //if we have the encoder, get it to return
            if (encoders.ContainsKey(lookupKey))
            {
                //pull the codec from the lookup
                foundCodec = encoders[lookupKey];
            }

            return foundCodec;
        }

        private

         Dictionary<string, ImageCodecInfo> Encoders()
        {
            Dictionary<string, ImageCodecInfo> encoders = new Dictionary<string, ImageCodecInfo>();
            //get accessor that creates the dictionary on demand

            //get all the codecs
            foreach (ImageCodecInfo codec in ImageCodecInfo.GetImageEncoders())
            {
                //add each codec to the quick lookup
                encoders.Add(codec.MimeType.ToLower(), codec);
            }
            //return the lookup
            return encoders;

        }


        void IDisposable.Dispose()
        {
            if (_graphics != null) _graphics.Dispose();
            if (_bitmap != null) _bitmap.Dispose();
        }
        
    }
}
