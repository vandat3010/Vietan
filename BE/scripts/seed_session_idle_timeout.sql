-- BE 2.1a / T3.1 — Idle-timeout configuration for FE auto-logout.
-- The value is consumed by the FRONTEND (idle timer → POST /logout); the backend
-- only stores/serves it via GET /api/v1/app-settings/by-key/session.idleTimeoutMinutes.
-- Idempotent: the UNIQUE(setting_key) index means re-running never duplicates,
-- and ON CONFLICT DO NOTHING never overwrites an operator-tuned value.
-- Run: psql -h localhost -U postgres -d <db> -f scripts/seed_session_idle_timeout.sql

INSERT INTO scada.app_settings
    (setting_key, setting_value, data_type, description, is_enable, created_at, updated_at)
VALUES
    ('session.idleTimeoutMinutes', '10', 'int',
     'Minutes of user inactivity before the frontend auto-logs-out. Normal users only; admins are exempt by business rule.',
     TRUE, now(), now())
ON CONFLICT (setting_key) DO NOTHING;
