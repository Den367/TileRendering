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
BEGIN TRAN
DROP FUNCTION [import].[ExcludeShortLine]
GO

DROP FUNCTION [import].[clr_GetSector]
GO

DROP FUNCTION [import].[clr_GetSectorVarAnglePitch]
GO


DROP ASSEMBLY [Import.GeometryUtility] 
GO

CREATE ASSEMBLY [Import.GeometryUtility] 
from 'c:\GeometryUtility.dll' WITH PERMISSION_SET = SAFE;
GO
CREATE FUNCTION [import].[ExcludeShortLine] (@shape [sys].[geometry], @shortestDistance [float])
RETURNS [sys].[geometry]
AS EXTERNAL NAME [Import.GeometryUtility].[Simplifier.ShortLineExcluding].[ExcludeShortLine];

--GO
GO
CREATE FUNCTION [import].[clr_GetSector] (@longiutde float, @latitude float, @azimuth float, @angle float, @radius float)
RETURNS [sys].[geometry]
AS EXTERNAL NAME [Import.GeometryUtility].[PST.GeoSpatial.Drawing.GeoSpatialBuilder].[DrawGeoSpatialSector];

GO

CREATE FUNCTION [import].[clr_GetSectorVarAnglePitch] (@longiutde float, @latitude float, @azimuth float, @angle float, @radius float, @stepangle float)
RETURNS [sys].[geometry]
AS EXTERNAL NAME [Import.GeometryUtility].[PST.GeoSpatial.Drawing.GeoSpatialBuilder].[DrawGeoSpatialSectorVarAngle];
GO
--COMMIT TRAN
GO

SELECT [import].[clr_GetSectorVarAnglePitch](63.50, 70.60, 10, 360, 2000, 5)