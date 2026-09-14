-- IAM entity-change trail (EF maps AuditLog → app."AuditLogs").
-- Run manually when Database:ApplyEfMigrations = false.

CREATE SCHEMA IF NOT EXISTS app;

CREATE TABLE IF NOT EXISTS app."AuditLogs"
(
    "Id"             uuid PRIMARY KEY,
    "EntityName"     varchar(100) NOT NULL,
    "EntityId"       varchar(100) NULL,
    "Action"         varchar(50)  NOT NULL,
    "UserId"         varchar(100) NULL,
    "UserName"       varchar(100) NULL,
    "OldValues"      text NULL,
    "NewValues"      text NULL,
    "IpAddress"      varchar(64) NULL,
    "CorrelationId"  varchar(100) NULL,
    "CreatedDate"    timestamptz NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
    "ModifiedDate"   timestamptz NULL
);

CREATE INDEX IF NOT EXISTS "IX_AuditLogs_CreatedDate" ON app."AuditLogs" ("CreatedDate");
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_EntityName_EntityId" ON app."AuditLogs" ("EntityName", "EntityId");
