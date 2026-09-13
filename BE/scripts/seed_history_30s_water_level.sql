-- Tạo history.history_30s (Timescale) + seed báo cáo mức nước
BEGIN;

CREATE TABLE IF NOT EXISTS history.history_30s (
  time    timestamptz       NOT NULL,
  tag_id  bigint            NOT NULL,
  value   double precision  NOT NULL,
  PRIMARY KEY (time, tag_id)
);

CREATE INDEX IF NOT EXISTS ix_history_30s_tag_time
  ON history.history_30s (tag_id, time DESC);

-- Hypertable (bỏ qua nếu đã là hypertable / chưa cài timescaledb)
DO $$
BEGIN
  PERFORM create_hypertable('history.history_30s', 'time', if_not_exists => TRUE);
EXCEPTION
  WHEN undefined_function THEN NULL;
  WHEN OTHERS THEN NULL;
END $$;

-- Đảm bảo tag mức xả 1..10 (device 1)
WITH defs(ord, suffix, tag_name, display_name) AS (
  VALUES
    (1,  'LEVEL_DISCHARGE_1',  'LevelDischarge1',  'Mức xả 1'),
    (2,  'LEVEL_DISCHARGE_2',  'LevelDischarge2',  'Mức xả 2'),
    (3,  'LEVEL_DISCHARGE_3',  'LevelDischarge3',  'Mức xả 3'),
    (4,  'LEVEL_DISCHARGE_4',  'LevelDischarge4',  'Mức xả 4'),
    (5,  'LEVEL_DISCHARGE_5',  'LevelDischarge5',  'Mức xả 5'),
    (6,  'LEVEL_DISCHARGE_6',  'LevelDischarge6',  'Mức xả 6'),
    (7,  'LEVEL_DISCHARGE_7',  'LevelDischarge7',  'Mức xả 7'),
    (8,  'LEVEL_DISCHARGE_8',  'LevelDischarge8',  'Mức xả 8'),
    (9,  'LEVEL_DISCHARGE_9',  'LevelDischarge9',  'Mức xả 9'),
    (10, 'LEVEL_DISCHARGE_10', 'LevelDischarge10', 'Mức xả 10')
),
target AS (
  SELECT id AS device_id, plc_id FROM scada.device WHERE id = 1
)
INSERT INTO scada.tag (
  id, plc_id, device_id, code, tag, display_name, address, data_type,
  unit, scale, offset_value, read_only, write_enable, enable_realtime, enable_alarm,
  description, created_at, updated_at
)
SELECT
  2220000 + defs.ord,
  t.plc_id,
  t.device_id,
  'D1_' || defs.suffix,
  defs.tag_name,
  defs.display_name,
  'DB4.DBD' || (defs.ord * 4),
  'Float',
  'm', 1.0, 0.0, true, false, true, false,
  'Water-level report seed',
  NOW(), NOW()
FROM target t
CROSS JOIN defs
ON CONFLICT (code) DO UPDATE SET
  tag = EXCLUDED.tag,
  display_name = EXCLUDED.display_name,
  updated_at = NOW();

-- Seed history_30s: 01/06/2026 mỗi 30s trong 10 phút đầu + vài mẫu hôm nay
WITH times AS (
  SELECT ts FROM generate_series(
    timestamptz '2026-06-01 00:00:00+07',
    timestamptz '2026-06-01 00:09:30+07',
    interval '30 seconds'
  ) AS ts
  UNION ALL
  SELECT date_trunc('minute', NOW()) - (g.i * interval '30 seconds')
  FROM generate_series(0, 9) AS g(i)
),
tags AS (
  SELECT id, code FROM scada.tag
  WHERE code IN (
    'D1_LEVEL_RIVER',
    'D1_LEVEL_DISCHARGE_1','D1_LEVEL_DISCHARGE_2','D1_LEVEL_DISCHARGE_3','D1_LEVEL_DISCHARGE_4','D1_LEVEL_DISCHARGE_5',
    'D1_LEVEL_DISCHARGE_6','D1_LEVEL_DISCHARGE_7','D1_LEVEL_DISCHARGE_8','D1_LEVEL_DISCHARGE_9','D1_LEVEL_DISCHARGE_10'
  )
)
INSERT INTO history.history_30s (time, tag_id, value)
SELECT
  times.ts,
  tags.id,
  CASE
    WHEN tags.code = 'D1_LEVEL_RIVER' THEN 2.10 + (EXTRACT(SECOND FROM times.ts)::int / 30) * 0.01
      + (EXTRACT(MINUTE FROM times.ts)::int % 5) * 0.02
    ELSE 1.20
      + COALESCE(NULLIF(regexp_replace(tags.code, '.*_(\d+)$', '\1'), tags.code)::int, 0) * 0.05
      + (EXTRACT(SECOND FROM times.ts)::int / 30) * 0.01
  END
FROM times
CROSS JOIN tags
ON CONFLICT DO NOTHING;

COMMIT;
