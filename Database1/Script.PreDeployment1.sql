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
CREATE ASSEMBLY SqlBitmapOperation
FROM 'D:\denism\tfs_web\TeleGISIntegration\Src\GeoDataTool\Database1\'

WITH PERMISSION_SET = SAFE
GO



CREATE AGGREGATE [import].[OrderedCoords2LineStringAgg]
(@X [float], @Y [float], @order [int])
RETURNS[geometry]
EXTERNAL NAME [LineStringAgg].[TFolSql.OrderedCoords2LineStringAgg]
GO


CREATE AGGREGATE [import].[Coords2LineStringAgg]
(@X [float], @Y [float])
RETURNS[geometry]
EXTERNAL NAME [LineStringAgg].[TFolSql.Coords2LineStringAgg]
GO






