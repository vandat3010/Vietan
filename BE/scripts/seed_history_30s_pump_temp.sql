-- Seed history_30s cho bao cao nhiet do bom (device 1) ngay 2026-06-01
BEGIN;

WITH times AS (
  SELECT ts FROM generate_series(
    timestamptz '2026-06-01 00:00:00+07',
    timestamptz '2026-06-01 00:04:30+07',
    interval '30 seconds'
  ) AS ts
),
tags AS (
  SELECT id, code FROM scada.tag
  WHERE device_id = 1 AND (
    code ~ 'TEMP_COIL_[ABC]$' OR code ~ 'BEARING_(TOP|BOTTOM)$'
  )
)
INSERT INTO history.history_30s (time, tag_id, value)
SELECT
  times.ts,
  tags.id,
  CASE
    WHEN tags.code LIKE '%TEMP_COIL_A' THEN 31 + (EXTRACT(MINUTE FROM times.ts) % 3)
    WHEN tags.code LIKE '%TEMP_COIL_B' THEN 32 + (EXTRACT(MINUTE FROM times.ts) % 3)
    WHEN tags.code LIKE '%TEMP_COIL_C' THEN 33 + (EXTRACT(MINUTE FROM times.ts) % 3)
    WHEN tags.code LIKE '%BEARING_TOP' THEN 45 + (EXTRACT(SECOND FROM times.ts)::int / 30)
    WHEN tags.code LIKE '%BEARING_BOTTOM' THEN 43 + (EXTRACT(SECOND FROM times.ts)::int / 30)
    ELSE 0
  END
FROM times
CROSS JOIN tags
ON CONFLICT DO NOTHING;

COMMIT;
