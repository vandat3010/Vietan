-- Seed missing device-card tags: allowed setpoints + runtime instant
BEGIN;

WITH defs(ord, suffix, tag_name, display_name, unit) AS (
  VALUES
    (1, 'TEMP_COIL_A_MAX', 'TempCoilAMax', 'Nhiet do cuon A cho phep', 'C'),
    (2, 'TEMP_COIL_B_MAX', 'TempCoilBMax', 'Nhiet do cuon B cho phep', 'C'),
    (3, 'TEMP_COIL_C_MAX', 'TempCoilCMax', 'Nhiet do cuon C cho phep', 'C'),
    (4, 'BEARING_TOP_MAX', 'BearingTopMax', 'O bi tren cho phep', 'C'),
    (5, 'BEARING_BOTTOM_MAX', 'BearingBottomMax', 'O bi duoi cho phep', 'C'),
    (6, 'RUNTIME_INSTANT', 'RuntimeInstant', 'Thoi gian chay tuc thoi', 'h')
),
targets(device_id, plc_id, id_base) AS (
  SELECT 1, plc_id, 2210000 FROM scada.device WHERE id = 1
  UNION ALL
  SELECT 31, plc_id, 2210100 FROM scada.device WHERE id = 31
)
INSERT INTO scada.tag (
  id, plc_id, device_id, code, tag, display_name, address, data_type,
  unit, scale, offset_value, read_only, write_enable, enable_realtime, enable_alarm,
  description, created_at, updated_at
)
SELECT
  t.id_base + defs.ord,
  t.plc_id,
  t.device_id,
  'D' || t.device_id::text || '_' || defs.suffix,
  defs.tag_name,
  defs.display_name,
  'DB3.DBD' || (defs.ord * 4),
  'Float',
  defs.unit,
  1.0, 0.0, true, false, true, false,
  'Device-card seed',
  NOW(), NOW()
FROM targets t
CROSS JOIN defs
ON CONFLICT (code) DO UPDATE SET
  tag = EXCLUDED.tag,
  display_name = EXCLUDED.display_name,
  unit = EXCLUDED.unit,
  updated_at = NOW();

-- History: allowed setpoints (same for both devices)
INSERT INTO history.history_1s (time, tag_id, value)
SELECT NOW() - INTERVAL '20 seconds', tg.id, v.value
FROM (VALUES
  (1, 2210001, 80.0), (1, 2210002, 80.0), (1, 2210003, 80.0),
  (1, 2210004, 70.0), (1, 2210005, 70.0),
  (31, 2210101, 80.0), (31, 2210102, 80.0), (31, 2210103, 80.0),
  (31, 2210104, 70.0), (31, 2210105, 70.0)
) AS v(device_id, tag_id, value)
JOIN scada.tag tg ON tg.id = v.tag_id;

-- Runtime instant: device 1 ~3h20', device 31 stopped
INSERT INTO history.history_1s (time, tag_id, value)
SELECT NOW() - INTERVAL '15 seconds', id,
  CASE WHEN device_id = 1 THEN 3.333 ELSE 0.0 END
FROM scada.tag
WHERE code IN ('D1_RUNTIME_INSTANT', 'D31_RUNTIME_INSTANT');

-- Runtime total (schematic RUNTIME tag)
INSERT INTO history.history_1s (time, tag_id, value)
SELECT NOW() - INTERVAL '10 seconds', id,
  CASE WHEN device_id = 1 THEN 348.833 ELSE 120.5 END
FROM scada.tag
WHERE code IN ('D1_RUNTIME', 'D31_RUNTIME');

COMMIT;
