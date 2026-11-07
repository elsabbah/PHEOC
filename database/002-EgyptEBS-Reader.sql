-- Optional, explicit DBA action. This script changes access permissions only.
-- It does not change EgyptEBS data, tables, views, or procedures.
-- Run only after approval to create the mapped read-only user in EgyptEBS.
USE [EgyptEBS];
GO
IF USER_ID(N'pheoc_oms_app') IS NULL
    CREATE USER [pheoc_oms_app] FOR LOGIN [pheoc_oms_app];
GO
GRANT SELECT ON OBJECT::dbo.Signals TO [pheoc_oms_app];
GRANT SELECT ON OBJECT::dbo.Verifications TO [pheoc_oms_app];
GRANT SELECT ON OBJECT::dbo.YesNoCodeTables TO [pheoc_oms_app];
GRANT SELECT ON OBJECT::dbo.Triages TO [pheoc_oms_app];
GRANT SELECT ON OBJECT::dbo.RelevanceCodeTables TO [pheoc_oms_app];
GRANT SELECT ON OBJECT::dbo.SignalTypes TO [pheoc_oms_app];
GRANT SELECT ON OBJECT::dbo.SignalCategoryCodeTables TO [pheoc_oms_app];
GRANT SELECT ON OBJECT::dbo.SignalLocations TO [pheoc_oms_app];
GRANT SELECT ON OBJECT::dbo.AdministrativeLevels TO [pheoc_oms_app];
GRANT SELECT ON OBJECT::dbo.AdministrativeLevelCodeTables TO [pheoc_oms_app];
GO
