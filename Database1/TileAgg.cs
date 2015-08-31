//------------------------------------------------------------------------------
// <copyright file="CSSqlAggregate.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using TileRendering;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;


[Serializable]
[SqlUserDefinedAggregate(Format.UserDefined, IsInvariantToDuplicates = true, IsInvariantToNulls = true, IsInvariantToOrder = false, IsNullIfEmpty = false, MaxByteSize = -1)]
public struct TileAgg : IBinarySerialize
{

    Bitmap _bitmap;
    ImageFormat _format;
    Graphics _graphics;
    ImageCodecInfo _codecInfo;
    const int TILE_SIZE = 256;
   
    /// <summary>
    /// создаЄт начальный тайл
    /// </summary>
    /// <returns></returns>
    Bitmap GetInitialTile()
    {

        Bitmap DrawArea = new Bitmap(TILE_SIZE, TILE_SIZE);
        // инициализируем прозрачную картинку
        using (Graphics xGraph = Graphics.FromImage(DrawArea))
        {
            xGraph.FillRectangle(Brushes.Transparent, 0, 0, TILE_SIZE, TILE_SIZE);
            _graphics = Graphics.FromImage(DrawArea);
            return DrawArea;
        }
    }

    #region [Aggregate artifacts]

      public void Init()
    {
        _codecInfo = GetEncoderInfo("image/png");
        _bitmap = GetInitialTile();
        DetectFormat(); // об€зательное определение формата
    }

    public void Accumulate(SqlBinary Value)
    {
        // добавл€ем новую запись к результату
        using (MemoryStream ms = new MemoryStream())
        {
            ms.Write(Value.Value, 0, Value.Length);
            ms.Seek(0, SeekOrigin.Begin);
            ms.Position = 0;          
            PasteFromStreamImageToTile( ms); 
        }
    }

    public void Merge(TileAgg Group)
    {
        PasteGroup(Group.Terminate());
    }

    public SqlBinary Terminate()
    {
        return GetBytes();
    }

#endregion [Aggregate artifacts]

    void PasteFromStreamImageToTile( Stream stream)
    {
        using (Bitmap iconImage = new Bitmap(stream, false))
        {
            DetectFormat();
            int width = iconImage.Width;
            int height = iconImage.Height;
            var area = new Rectangle(0, 0, width, height);
            CopyRegionIntoImage(iconImage,area, area);
        }
    }

    /// <summary>
    ///  опируем картинку <see cref="Bitmap"/> srcBitmap на итоговую картинку
    /// </summary>
    /// <param name="srcBitmap"></param>
    /// <param name="srcRegion"></param>
    /// <param name="destRegion"></param>
    void CopyRegionIntoImage(Bitmap srcBitmap, Rectangle srcRegion, Rectangle destRegion)
    {
        _graphics.DrawImage(srcBitmap, destRegion, srcRegion, GraphicsUnit.Pixel);
        srcBitmap.Dispose();
    }
  
    void PasteGroup(SqlBinary Value)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            ms.Write(Value.Value, 0, Value.Length);
            ms.Seek(0, SeekOrigin.Begin);
            ms.Position = 0;
            PasteTile(ms);
        }
    }

    void PasteTile(Stream stream)
    {
        Rectangle bounds = new Rectangle(0, 0, TILE_SIZE, TILE_SIZE);
        CopyRegionIntoImage(new Bitmap(stream), bounds, bounds);
    }

  
/// <summary>
/// возвращает сериализованный итоговый тайл
/// </summary>
/// <returns></returns>
    byte[] GetBytes()
    {
        return _bitmap.ToByteArray(ImageFormat.Png);
    }

    #region [IBinarySerialize]
    public void Read(BinaryReader reader)
    {
        _bitmap = new Bitmap(new MemoryStream(reader.ReadBytes((int)reader.BaseStream.Length)));
        DetectFormat();
    }

    public void Write(BinaryWriter writer)
    {
        EncoderParameters encodeParams = new EncoderParameters(1);
        encodeParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100);
        _bitmap.Save(writer.BaseStream, _codecInfo, encodeParams);
    }
    #endregion [IBinarySerialize]

    /// <summary>
    /// Needs
    /// </summary>
    void DetectFormat()
    {
        _format = _bitmap.GetImageFormat();
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

    private Dictionary<string, ImageCodecInfo> Encoders()
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
}
