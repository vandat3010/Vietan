-- Bootstrap for scada_tlhn (ERD public + app extensions).
-- Idempotent — safe to re-run.

CREATE SCHEMA IF NOT EXISTS app;

-- ERD Users: security / profile extensions required by auth flows.
ALTER TABLE public."Users" ADD COLUMN IF NOT EXISTS "LastLoginAt" timestamptz NULL;
ALTER TABLE public."Users" ADD COLUMN IF NOT EXISTS "Unit" text NULL;
ALTER TABLE public."Users" ADD COLUMN IF NOT EXISTS "Level" integer NULL;
ALTER TABLE public."Users" ADD COLUMN IF NOT EXISTS "Department" text NULL;
ALTER TABLE public."Users" ADD COLUMN IF NOT EXISTS "Position" text NULL;
ALTER TABLE public."Users" ADD COLUMN IF NOT EXISTS "Description" text NULL;
ALTER TABLE public."Users" ADD COLUMN IF NOT EXISTS "CreatedBy" text NULL;
ALTER TABLE public."Users" ADD COLUMN IF NOT EXISTS "UpdatedBy" text NULL;
ALTER TABLE public."Users" ADD COLUMN IF NOT EXISTS "MustChangePassword" boolean NOT NULL DEFAULT false;
ALTER TABLE public."Users" ADD COLUMN IF NOT EXISTS "PasswordUpdatedAt" timestamptz NULL;
ALTER TABLE public."Users" ADD COLUMN IF NOT EXISTS "FailedLoginCount" integer NOT NULL DEFAULT 0;
ALTER TABLE public."Users" ADD COLUMN IF NOT EXISTS "LockoutUntil" timestamptz NULL;

-- Device optional FK to DeviceType.
ALTER TABLE public."Device" ADD COLUMN IF NOT EXISTS "DeviceTypeId" integer NULL;

-- Tag IsActive extension.
ALTER TABLE public."Tag" ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT true;

-- history_30s (missing on live ERD dump).
CREATE TABLE IF NOT EXISTS public.history_30s (
    "Time" timestamptz NOT NULL,
    "TagId" bigint NOT NULL,
    "Value" double precision NOT NULL,
    PRIMARY KEY ("Time", "TagId")
);
CREATE INDEX IF NOT EXISTS "IX_history_30s_TagId_Time" ON public.history_30s ("TagId", "Time");

-- Soften UserActivityLogs.UserName to allow system rows without actor.
ALTER TABLE public."UserActivityLogs" ALTER COLUMN "UserName" DROP NOT NULL;

-- App extensions beyond ERD.
CREATE TABLE IF NOT EXISTS app.refresh_tokens (
    id bigserial PRIMARY KEY,
    user_id bigint NOT NULL REFERENCES public."Users"("Id") ON DELETE CASCADE,
    token_hash varchar(128) NOT NULL,
    expires_at timestamptz NOT NULL,
    revoked_at timestamptz NULL,
    revoked_by_ip varchar(50) NULL,
    created_by_ip varchar(50) NULL,
    replaced_by_token_id bigint NULL,
    session_id varchar(64) NULL,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_refresh_tokens_token_hash" ON app.refresh_tokens (token_hash);
CREATE INDEX IF NOT EXISTS "IX_refresh_tokens_user_id" ON app.refresh_tokens (user_id);
CREATE INDEX IF NOT EXISTS "IX_refresh_tokens_session_id" ON app.refresh_tokens (session_id);

CREATE TABLE IF NOT EXISTS app.password_reset_tokens (
    id bigserial PRIMARY KEY,
    user_id bigint NOT NULL REFERENCES public."Users"("Id") ON DELETE CASCADE,
    token_hash varchar(128) NOT NULL,
    expires_at timestamptz NOT NULL,
    used_at timestamptz NULL,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_password_reset_tokens_token_hash" ON app.password_reset_tokens (token_hash);
CREATE INDEX IF NOT EXISTS "IX_password_reset_tokens_user_id" ON app.password_reset_tokens (user_id);

CREATE TABLE IF NOT EXISTS app.system_audit_logs (
    id bigserial PRIMARY KEY,
    action varchar(100) NOT NULL,
    event_type varchar(50) NOT NULL,
    status varchar(20) NOT NULL,
    user_id bigint NULL,
    user_name varchar(100) NULL,
    description text NULL,
    module varchar(100) NULL,
    endpoint varchar(300) NULL,
    http_method varchar(10) NULL,
    http_status_code integer NULL,
    ip_address varchar(64) NULL,
    user_agent varchar(512) NULL,
    entity_type varchar(100) NULL,
    entity_id varchar(100) NULL,
    correlation_id varchar(100) NULL,
    additional_data jsonb NULL,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_system_audit_logs_created_at" ON app.system_audit_logs (created_at);
CREATE INDEX IF NOT EXISTS "IX_system_audit_logs_action" ON app.system_audit_logs (action);
CREATE INDEX IF NOT EXISTS "IX_system_audit_logs_user_name" ON app.system_audit_logs (user_name);

CREATE TABLE IF NOT EXISTS app.app_settings (
    id bigserial PRIMARY KEY,
    setting_key varchar(100) NOT NULL,
    setting_value text NULL,
    data_type varchar(50) NOT NULL,
    description text NULL,
    is_enable boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_app_settings_setting_key" ON app.app_settings (setting_key);

CREATE TABLE IF NOT EXISTS app.system_licenses (
    "Id" uuid PRIMARY KEY,
    "LicenseKey" varchar(200) NOT NULL,
    "MaxConcurrentUsers" integer NOT NULL,
    "IsEnabled" boolean NOT NULL DEFAULT true,
    "Description" varchar(500) NULL,
    "ValidFrom" timestamptz NULL,
    "ValidTo" timestamptz NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT now(),
    "CreatedBy" varchar(100) NULL,
    "ModifiedAt" timestamptz NULL,
    "ModifiedBy" varchar(100) NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamptz NULL,
    "DeletedBy" varchar(100) NULL
);

CREATE TABLE IF NOT EXISTS app.tag_screen_mapping (
    id bigserial PRIMARY KEY,
    tag_id bigint NOT NULL REFERENCES public."Tag"("Id") ON DELETE CASCADE,
    screen_type integer NOT NULL,
    is_realtime boolean NOT NULL DEFAULT true,
    mapping_label varchar(200) NULL,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_tag_screen_mapping_tag_screen" ON app.tag_screen_mapping (tag_id, screen_type);

CREATE TABLE IF NOT EXISTS app.communication_config (
    id bigserial PRIMARY KEY,
    code varchar(50) NOT NULL,
    name varchar(100) NOT NULL,
    protocol varchar(50) NOT NULL,
    description text NULL,
    is_enable boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_communication_config_code" ON app.communication_config (code);

CREATE TABLE IF NOT EXISTS app.mqtt_config (
    id bigserial PRIMARY KEY,
    code varchar(50) NOT NULL,
    name varchar(100) NOT NULL,
    broker varchar(255) NOT NULL,
    port integer NOT NULL,
    username varchar(100) NULL,
    password varchar(255) NULL,
    client_id varchar(100) NULL,
    topic_publish varchar(255) NULL,
    topic_subscribe varchar(255) NULL,
    keep_alive integer NOT NULL DEFAULT 60,
    qos integer NOT NULL DEFAULT 0,
    retain boolean NOT NULL DEFAULT false,
    use_tls boolean NOT NULL DEFAULT false,
    is_enable boolean NOT NULL DEFAULT true,
    description text NULL,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_mqtt_config_code" ON app.mqtt_config (code);

CREATE TABLE IF NOT EXISTS app.event_logs (
    id bigserial PRIMARY KEY,
    time timestamptz NOT NULL,
    level varchar(30) NOT NULL,
    module varchar(100) NOT NULL,
    message text NOT NULL,
    exception text NULL,
    machine varchar(100) NULL
);
CREATE INDEX IF NOT EXISTS "IX_event_logs_time" ON app.event_logs (time);

-- Minimal IAM placeholders under app (default EF schema) so model can open.
CREATE TABLE IF NOT EXISTS app."Users" (
    "Id" uuid PRIMARY KEY,
    "Email" varchar(256) NOT NULL,
    "FirstName" varchar(100) NOT NULL DEFAULT '',
    "LastName" varchar(100) NOT NULL DEFAULT '',
    "PasswordHash" varchar(500) NOT NULL DEFAULT '',
    "PhoneNumber" varchar(20) NULL,
    "Status" varchar(30) NOT NULL DEFAULT 'Active',
    "FailedLoginAttempts" integer NOT NULL DEFAULT 0,
    "LockoutEnd" timestamptz NULL,
    "LastLoginAt" timestamptz NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT now(),
    "CreatedBy" varchar(100) NULL,
    "ModifiedAt" timestamptz NULL,
    "ModifiedBy" varchar(100) NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamptz NULL,
    "DeletedBy" varchar(100) NULL
);

INSERT INTO app.app_settings (setting_key, setting_value, data_type, description, is_enable, created_at, updated_at)
SELECT 'session.idleTimeoutMinutes', '10', 'int', 'FE idle timeout (minutes)', true, now(), now()
WHERE NOT EXISTS (SELECT 1 FROM app.app_settings WHERE setting_key = 'session.idleTimeoutMinutes');
