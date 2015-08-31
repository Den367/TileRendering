/*
 Pre-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be executed before the build script.	
 Use SQLCMD syntax to include a file in the pre-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the pre-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/
USE [TeleGIS_db01]
GO
 IF OBJECT_ID ( '[tile].[IconTileAgg]', 'AF' ) IS NOT NULL  
 DROP AGGREGATE [tile].[IconTileAgg];


 IF OBJECT_ID ( '[tile].[TileAgg]', 'AF' ) IS NOT NULL  
 DROP AGGREGATE [tile].[TileAgg];

 IF OBJECT_ID ( '[tile].[IconTile]', 'FS' ) IS NOT NULL  
 DROP FUNCTION [tile].[IconTile];

 
 IF OBJECT_ID ( '[tile].[ShapeTile]', 'FS' ) IS NOT NULL  
 DROP FUNCTION [tile].[ShapeTile];

  IF OBJECT_ID ( '[tile].[SaveToFolderByZoomXY]', 'FS' ) IS NOT NULL  
 DROP FUNCTION [tile].[SaveToFolderByZoomXY];

   IF OBJECT_ID ( '[tile].[SaveToFolderByZoomXY8bpp]', 'FS' ) IS NOT NULL  
 DROP FUNCTION [tile].[SaveToFolderByZoomXY8bpp];

IF  EXISTS (SELECT * FROM sys.assemblies asms WHERE asms.name = N'SqlBitmapOperation' and is_user_defined = 1)
    DROP ASSEMBLY [SqlBitmapOperation]
GO


IF  EXISTS (SELECT * FROM sys.assemblies asms WHERE asms.name = N'nQuant.Core' and is_user_defined = 1)
DROP ASSEMBLY [nQuant.Core]
GO

IF  EXISTS (SELECT * FROM sys.assemblies asms WHERE asms.name = N'TileRendering' and is_user_defined = 1)
DROP ASSEMBLY [TileRendering]



GO
CREATE ASSEMBLY [nQuant.Core]
AUTHORIZATION [dbo]
FROM 'd:\Import\BIN\TileRendering\nQuant.Core.dll'
	WITH PERMISSION_SET = UNSAFE
GO

CREATE ASSEMBLY [TileRendering]
AUTHORIZATION [dbo]
FROM 'd:\Import\BIN\TileRendering\TileRendering.dll'
	WITH PERMISSION_SET = EXTERNAL_ACCESS

GO


CREATE ASSEMBLY SqlBitmapOperation
	FROM 'd:\Import\BIN\TileRendering\SqlBitmapOperation.dll'
	WITH PERMISSION_SET = EXTERNAL_ACCESS
GO



CREATE AGGREGATE [tile].[TileAgg]
(@Value [varbinary](max))
RETURNS[varbinary](max)
EXTERNAL NAME [SqlBitmapOperation].[TileAgg]
GO



CREATE AGGREGATE [tile].[IconTileAgg]
(@Value [varbinary](max), @PixelX [int], @PixelY [int])
RETURNS[varbinary](max)
EXTERNAL NAME [SqlBitmapOperation].[IconTileAgg]
GO



CREATE FUNCTION [tile].[IconTile](@image [varbinary](max), @zoom [int], @Lon [float], @Lat [float], @xTile [int], @yTile [int], @scale [float])
RETURNS [varbinary](max) WITH EXECUTE AS CALLER
AS 
EXTERNAL NAME [SqlBitmapOperation].[BitmapFunctions].[IconTile]
GO


--ShapeTile(SqlGeometry shape, SqlInt32 zoom,  SqlInt32 xTile, SqlInt32 yTile, SqlString argbFill,SqlString argbStroke,SqlInt32 strokeWidth)
CREATE FUNCTION [tile].[ShapeTile](@shape GEOMETRY, @zoom [int], @xTile [int], @yTile [int], @argbFill NVARCHAR(10),@argbStroke NVARCHAR(10), @strokeWidth INT)
RETURNS [varbinary](max) WITH EXECUTE AS CALLER
AS 
EXTERNAL NAME [SqlBitmapOperation].[BitmapFunctions].[ShapeTile]
GO



--SaveToFolderByZoomXY(SqlBinary image, SqlString rootFolderPath, SqlInt32 Zoom, SqlInt32 X,SqlInt32 Y)
CREATE FUNCTION tile.SaveToFolderByZoomXY(@image VARBINARY(MAX),@rootFolderPat NVARCHAR(512) , @Zoom [int], @xTile [int], @yTile [int])
RETURNS BIT WITH EXECUTE AS CALLER
AS 
EXTERNAL NAME [SqlBitmapOperation].[BitmapFunctions].[SaveToFolderByZoomXY]
GO


--SaveToFolderByZoomXY8bpp(SqlBinary image, SqlString rootFolderPath, SqlInt32 Zoom, SqlInt32 X,SqlInt32 Y)
CREATE FUNCTION tile.SaveToFolderByZoomXY8bpp(@image VARBINARY(MAX),@rootFolderPat NVARCHAR(512) , @Zoom [int], @xTile [int], @yTile [int])
RETURNS BIT WITH EXECUTE AS CALLER
AS 
EXTERNAL NAME [SqlBitmapOperation].[BitmapFunctions].[SaveToFolderByZoomXY8bpp]
GO




