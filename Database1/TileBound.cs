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
using TileRendering;

public partial class UserDefinedFunctions
{
    [Microsoft.SqlServer.Server.SqlFunction]
    public static SqlGeometry TileBound( SqlInt32 zoom, SqlInt32 xTile, SqlInt32 yTile)
    {

        Coord2PixelConversion bounder = new Coord2PixelConversion();
       
           return bounder.GetTileBound((int)xTile,(int)yTile,(int)zoom,0);
       
    }
}
