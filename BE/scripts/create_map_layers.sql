-- Map layers metadata (files stored via IFileStorageService under uploads/map-layers).
-- Run manually when Database:ApplyEfMigrations = false.

CREATE SCHEMA IF NOT EXISTS app;

CREATE TABLE IF NOT EXISTS app.map_layers
(
    "Id"            uuid PRIMARY KEY,
    "Name"          varchar(200)  NOT NULL,
    "FileName"      varchar(260)  NOT NULL,
    "StoragePath"   varchar(500)  NOT NULL,
    "ContentType"   varchar(120)  NOT NULL,
    "Opacity"       numeric(5,4)  NOT NULL DEFAULT 1,
    "Weight"        numeric(8,2)  NOT NULL DEFAULT 2,
    "Visible"       boolean       NOT NULL DEFAULT true,
    "Color"         varchar(32)   NOT NULL DEFAULT '#3388ff',
    "SortOrder"     integer       NOT NULL DEFAULT 0,
    "CreatedBy"     text          NULL,
    "ModifiedBy"    text          NULL,
    "IsDeleted"     boolean       NOT NULL DEFAULT false,
    "DeletedDate"   timestamp without time zone NULL,
    "DeletedBy"     text          NULL,
    "CreatedDate"   timestamp without time zone NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
    "ModifiedDate"  timestamp without time zone NULL
);

CREATE INDEX IF NOT EXISTS "IX_map_layers_IsDeleted" ON app.map_layers ("IsDeleted");
