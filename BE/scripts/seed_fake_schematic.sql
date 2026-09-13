-- Fake seed for schematic: MCCB I/V + runtime + status (station TB01 pumps 1 & 31)
BEGIN;

WITH defs(ord, suffix, tag_name, display_name, unit, value) AS (
  VALUES
    (1,  'I1', 'I1', 'Dòng pha I1', 'A', 12.5),
    (2,  'I2', 'I2', 'Dòng pha I2', 'A', 11.8),
    (3,  'I3', 'I3', 'Dòng pha I3', 'A', 12.1),
    (4,  'V1', 'V1', 'Điện áp pha V1', 'V', 414.5),
    (5,  'V2', 'V2', 'Điện áp pha V2', 'V', 412.7),
    (6,  'V3', 'V3', 'Điện áp pha V3', 'V', 415.8),
    (7,  'RUNTIME', 'Runtime', 'Thời gian chạy', 'h', 128.5),
    (8,  'MOTOR_STATUS', 'MotorStatus', 'Trạng thái motor', NULL, 1),
    (9,  'KDM_STATUS', 'KdmStatus', 'Trạng thái KĐM', NULL, 1),
    (10, 'LOCK_STATUS', 'LockStatus', 'Trạng thái khoá', NULL, 1),
    (11, 'RATED_KW', 'RatedPower', 'Công suất định mức', 'kW', 160)
),
targets(device_id, plc_id, id_base, motor_value, kdm_value, lock_value) AS (
  SELECT 1,  plc_id, 2200000, 1::float8, 1::float8, 0::float8 FROM scada.device WHERE id = 1
  UNION ALL
  SELECT 31, plc_id, 2200100, 3::float8, 3::float8, 1::float8 FROM scada.device WHERE id = 31
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
  'DB2.DBD' || (defs.ord * 4),
  'Float',
  defs.unit,
  1.0, 0.0, true, false, true, false,
  'Schematic seed — ' || defs.display_name,
  NOW(), NOW()
FROM targets t
CROSS JOIN defs
ON CONFLICT (code) DO UPDATE SET
  tag = EXCLUDED.tag,
  display_name = EXCLUDED.display_name,
  unit = EXCLUDED.unit,
  updated_at = NOW();

-- History samples with status overrides per device
INSERT INTO history.history_1s (time, tag_id, value)
SELECT NOW() - INTERVAL '30 seconds', tg.id,
       CASE
         WHEN d.suffix = 'MOTOR_STATUS' THEN t.motor_value
         WHEN d.suffix = 'KDM_STATUS' THEN t.kdm_value
         WHEN d.suffix = 'LOCK_STATUS' THEN t.lock_value
         ELSE d.value
       END
FROM (
  VALUES
    (1,  2200000, 1::float8, 1::float8, 0::float8),
    (31, 2200100, 3::float8, 3::float8, 1::float8)
) AS t(device_id, id_base, motor_value, kdm_value, lock_value)
CROSS JOIN (
  VALUES
    (1,'I1',12.5),(2,'I2',11.8),(3,'I3',12.1),
    (4,'V1',414.5),(5,'V2',412.7),(6,'V3',415.8),
    (7,'RUNTIME',128.5),(8,'MOTOR_STATUS',1),(9,'KDM_STATUS',1),
    (10,'LOCK_STATUS',0),(11,'RATED_KW',160)
) AS d(ord, suffix, value)
JOIN scada.tag tg ON tg.id = t.id_base + d.ord;

COMMIT;

SELECT code, tag, unit FROM scada.tag
WHERE code LIKE 'D1_I%' OR code LIKE 'D1_V%' OR code LIKE 'D1_MOTOR%' OR code LIKE 'D1_RUNTIME%'
ORDER BY code;
