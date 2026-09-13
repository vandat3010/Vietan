-- Fake seed: stations + electrical tags + history + related fields
-- Safe to re-run: uses ON CONFLICT / fixed IDs in high range

BEGIN;

-- ---------------------------------------------------------------------------
-- 1) Enrich existing stations (names/addresses closer to FE)
-- ---------------------------------------------------------------------------
UPDATE scada.station SET
  name = CASE code
    WHEN 'TB01' THEN 'Trạm Ấp Bắc'
    WHEN 'TB02' THEN 'Trạm Nội Bài'
    WHEN 'TB03' THEN 'Trạm Thanh Điềm'
    WHEN 'TB04' THEN 'Trạm Thịnh Liên'
    WHEN 'TB05' THEN 'Trạm Phù Đổng'
    WHEN 'TB06' THEN 'Trạm Tam Bảo'
    WHEN 'TB07' THEN 'Trạm Thạc Quả'
    WHEN 'TB08' THEN 'Trạm Đông Anh'
    WHEN 'TB09' THEN 'Trạm Gia Lâm'
    WHEN 'TB10' THEN 'Trạm Sóc Sơn'
    ELSE name
  END,
  address = CASE code
    WHEN 'TB01' THEN 'Thôn Võng La, xã Võng La, huyện Đông Anh, Hà Nội'
    WHEN 'TB02' THEN 'Nội Bài, Sóc Sơn, Hà Nội'
    WHEN 'TB03' THEN 'Thanh Điềm, Hà Nội'
    WHEN 'TB04' THEN 'Thịnh Liên, Hà Nội'
    WHEN 'TB05' THEN 'Phù Đổng, Gia Lâm, Hà Nội'
    WHEN 'TB06' THEN 'Tam Bảo, Hà Nội'
    WHEN 'TB07' THEN 'Thạc Quả, Hà Nội'
    WHEN 'TB08' THEN 'Đông Anh, Hà Nội'
    WHEN 'TB09' THEN 'Gia Lâm, Hà Nội'
    WHEN 'TB10' THEN 'Sóc Sơn, Hà Nội'
    ELSE address
  END,
  description = COALESCE(description, 'Trạm bơm tiêu — dữ liệu demo'),
  latitude = CASE code
    WHEN 'TB01' THEN 21.1500
    WHEN 'TB02' THEN 21.2185
    WHEN 'TB03' THEN 21.1200
    WHEN 'TB04' THEN 21.0800
    WHEN 'TB05' THEN 21.0600
    WHEN 'TB06' THEN 21.0300
    WHEN 'TB07' THEN 21.0100
    WHEN 'TB08' THEN 21.1400
    WHEN 'TB09' THEN 21.0500
    WHEN 'TB10' THEN 21.2600
    ELSE latitude
  END,
  longitude = CASE code
    WHEN 'TB01' THEN 105.6542
    WHEN 'TB02' THEN 105.8042
    WHEN 'TB03' THEN 105.7200
    WHEN 'TB04' THEN 105.6900
    WHEN 'TB05' THEN 105.9500
    WHEN 'TB06' THEN 105.8100
    WHEN 'TB07' THEN 105.7800
    WHEN 'TB08' THEN 105.8500
    WHEN 'TB09' THEN 105.9000
    WHEN 'TB10' THEN 105.8500
    ELSE longitude
  END,
  is_active = CASE WHEN code = 'TB10' THEN false ELSE true END,
  updated_at = NOW()
WHERE code IN ('TB01','TB02','TB03','TB04','TB05','TB06','TB07','TB08','TB09','TB10');

-- ---------------------------------------------------------------------------
-- 2) Insert extra stations (+ PLC + 1 Pump each)
-- ---------------------------------------------------------------------------
INSERT INTO scada.station (code, name, address, latitude, longitude, description, is_active, created_at, updated_at)
VALUES
  ('TB11', 'Trạm Mê Linh', 'Mê Linh, Hà Nội', 21.1800, 105.7200, 'Trạm demo — Mê Linh', true, NOW(), NOW()),
  ('TB12', 'Trạm Hoài Đức', 'Hoài Đức, Hà Nội', 21.0200, 105.7000, 'Trạm demo — Hoài Đức', true, NOW(), NOW()),
  ('TB13', 'Trạm Thanh Trì', 'Thanh Trì, Hà Nội', 20.9700, 105.8600, 'Trạm demo — Thanh Trì', true, NOW(), NOW()),
  ('TB14', 'Trạm Thường Tín', 'Thường Tín, Hà Nội', 20.8700, 105.8600, 'Trạm demo — Thường Tín', false, NOW(), NOW())
ON CONFLICT (code) DO UPDATE SET
  name = EXCLUDED.name,
  address = EXCLUDED.address,
  latitude = EXCLUDED.latitude,
  longitude = EXCLUDED.longitude,
  description = EXCLUDED.description,
  is_active = EXCLUDED.is_active,
  updated_at = NOW();

-- PLC for new stations (reuse pattern; codes unique)
INSERT INTO scada.plc (
  station_id, code, name, plc_type, ip_address, rack, slot, port,
  polling_interval, reconnect_interval, timeout, max_connection,
  is_enable, description, created_at, updated_at
)
SELECT s.id,
       'PLC_' || RIGHT(s.code, 2) || '_01',
       'PLC ' || s.name,
       'S7-1200',
       ('192.168.10.' || (10 + (RIGHT(s.code, 2)::int)))::inet,
       0, 1, 102,
       1000, 5000, 3000, 1,
       true,
       'PLC demo cho ' || s.code,
       NOW(), NOW()
FROM scada.station s
WHERE s.code IN ('TB11','TB12','TB13','TB14')
ON CONFLICT (code) DO NOTHING;

-- One Pump device per new PLC
INSERT INTO scada.device (
  plc_id, code, name, display_name, device_type, description, is_enable, created_at, updated_at
)
SELECT p.id,
       'PUMP_' || p.code,
       'Bơm 1 - ' || s.name,
       'Bơm 1 - ' || s.name,
       'Pump',
       'Thiết bị bơm demo',
       true,
       NOW(), NOW()
FROM scada.plc p
JOIN scada.station s ON s.id = p.station_id
WHERE s.code IN ('TB11','TB12','TB13','TB14')
  AND p.code LIKE 'PLC_%_01'
ON CONFLICT (code) DO NOTHING;

-- ---------------------------------------------------------------------------
-- 3) Electrical tags for station TB01 pumps (device 1 = Pump, and device 31)
--    Also enrich device 1 display name
-- ---------------------------------------------------------------------------
UPDATE scada.device
SET display_name = 'Bơm 1 - Trạm Ấp Bắc',
    name = 'Bơm 1',
    description = 'Bơm tiêu chính — demo thông số điện',
    updated_at = NOW()
WHERE id = 1;

UPDATE scada.device
SET display_name = 'Bơm 2 - Trạm Ấp Bắc',
    name = 'Bơm 2',
    device_type = 'Pump',
    description = 'Bơm tiêu phụ — demo thông số điện',
    updated_at = NOW()
WHERE id = 31;

-- Normalize existing current tag so FE key currentA matches cleanly
UPDATE scada.tag
SET code = 'CURRENT',
    tag = 'Current',
    display_name = 'Dòng điện',
    unit = 'A',
    description = 'Dòng điện pha — demo',
    updated_at = NOW()
WHERE id = 1000001;

-- Helper: insert electrical tag set for a device
-- IDs: 2100001+ for device 1, 2100101+ for device 31, 2100201+ for new pumps

WITH defs(ord, code_suffix, tag_name, display_name, unit, data_type) AS (
  VALUES
    (1, 'VOLTAGE_RS', 'VoltageRS', 'Điện áp dây RS', 'V', 'Float'),
    (2, 'VOLTAGE_ST', 'VoltageST', 'Điện áp dây ST', 'V', 'Float'),
    (3, 'VOLTAGE_TR', 'VoltageTR', 'Điện áp dây TR', 'V', 'Float'),
    (4, 'POWER_FACTOR', 'PowerFactor', 'Hệ số công suất', NULL, 'Float'),
    (5, 'FREQ', 'Frequency', 'Tần số', 'Hz', 'Float'),
    (6, 'POWER', 'Power', 'Công suất', 'kW', 'Float'),
    (7, 'ENERGY', 'Energy', 'Điện năng tiêu thụ', 'kWh', 'Float'),
    (8, 'TEMP_COIL_A', 'TempCoilA', 'Nhiệt độ cuộn A', '°C', 'Float'),
    (9, 'TEMP_COIL_B', 'TempCoilB', 'Nhiệt độ cuộn B', '°C', 'Float'),
    (10,'TEMP_COIL_C', 'TempCoilC', 'Nhiệt độ cuộn C', '°C', 'Float'),
    (11,'BEARING_TOP', 'BearingTop', 'Nhiệt độ ổ bi trên', '°C', 'Float'),
    (12,'BEARING_BOTTOM', 'BearingBottom', 'Nhiệt độ ổ bi dưới', '°C', 'Float'),
    (13,'LEVEL_RIVER', 'LevelRiver', 'Mực nước sông', 'm', 'Float'),
    (14,'LEVEL_BASIN', 'LevelBasin', 'Mực nước bể xả', 'm', 'Float')
),
targets(device_id, plc_id, id_base, code_prefix) AS (
  SELECT d.id, d.plc_id, 2100000, 'D' || d.id::text || '_'
  FROM scada.device d WHERE d.id = 1
  UNION ALL
  SELECT d.id, d.plc_id, 2100100, 'D' || d.id::text || '_'
  FROM scada.device d WHERE d.id = 31
  UNION ALL
  SELECT d.id, d.plc_id, 2100000 + (d.id * 100), 'D' || d.id::text || '_'
  FROM scada.device d
  JOIN scada.plc p ON p.id = d.plc_id
  JOIN scada.station s ON s.id = p.station_id
  WHERE s.code IN ('TB11','TB12') AND d.device_type = 'Pump'
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
  t.code_prefix || defs.code_suffix,
  defs.tag_name,
  defs.display_name,
  'DB1.DBD' || (defs.ord * 4),
  defs.data_type,
  defs.unit,
  1.0,
  0.0,
  true,
  false,
  true,
  defs.code_suffix IN ('VOLTAGE_RS','CURRENT','POWER','TEMP_COIL_A'),
  'Fake seed — ' || defs.display_name,
  NOW(),
  NOW()
FROM targets t
CROSS JOIN defs
ON CONFLICT (code) DO UPDATE SET
  tag = EXCLUDED.tag,
  display_name = EXCLUDED.display_name,
  unit = EXCLUDED.unit,
  enable_realtime = EXCLUDED.enable_realtime,
  description = EXCLUDED.description,
  updated_at = NOW();

-- CURRENT tag for device 31 if missing
INSERT INTO scada.tag (
  id, plc_id, device_id, code, tag, display_name, address, data_type,
  unit, scale, offset_value, read_only, write_enable, enable_realtime, enable_alarm,
  description, created_at, updated_at
)
SELECT
  2100115, d.plc_id, d.id, 'D31_CURRENT', 'Current', 'Dòng điện', 'DB1.DBD60', 'Float',
  'A', 1.0, 0.0, true, false, true, false, 'Fake seed — Dòng điện', NOW(), NOW()
FROM scada.device d WHERE d.id = 31
ON CONFLICT (code) DO NOTHING;

-- CURRENT for TB11/TB12 pumps
INSERT INTO scada.tag (
  id, plc_id, device_id, code, tag, display_name, address, data_type,
  unit, scale, offset_value, read_only, write_enable, enable_realtime, enable_alarm,
  description, created_at, updated_at
)
SELECT
  (2100000 + (d.id * 100) + 15),
  d.plc_id,
  d.id,
  'D' || d.id::text || '_CURRENT',
  'Current',
  'Dòng điện',
  'DB1.DBD60',
  'Float',
  'A', 1.0, 0.0, true, false, true, false,
  'Fake seed — Dòng điện', NOW(), NOW()
FROM scada.device d
JOIN scada.plc p ON p.id = d.plc_id
JOIN scada.station s ON s.id = p.station_id
WHERE s.code IN ('TB11','TB12') AND d.device_type = 'Pump'
ON CONFLICT (code) DO NOTHING;

-- ---------------------------------------------------------------------------
-- 4) Fake latest history_1s samples (within last day so API 7-day window hits)
-- ---------------------------------------------------------------------------
WITH samples(code, value) AS (
  VALUES
    ('CURRENT', 48.5),
    ('D1_VOLTAGE_RS', 381.2),
    ('D1_VOLTAGE_ST', 379.8),
    ('D1_VOLTAGE_TR', 380.5),
    ('D1_POWER_FACTOR', 0.92),
    ('D1_FREQ', 50.01),
    ('D1_POWER', 142.6),
    ('D1_ENERGY', 12850.4),
    ('D1_TEMP_COIL_A', 62.3),
    ('D1_TEMP_COIL_B', 61.8),
    ('D1_TEMP_COIL_C', 63.1),
    ('D1_BEARING_TOP', 45.2),
    ('D1_BEARING_BOTTOM', 43.7),
    ('D1_LEVEL_RIVER', 2.15),
    ('D1_LEVEL_BASIN', 1.42),
    ('D31_VOLTAGE_RS', 378.0),
    ('D31_VOLTAGE_ST', 377.5),
    ('D31_VOLTAGE_TR', 379.1),
    ('D31_POWER_FACTOR', 0.88),
    ('D31_FREQ', 49.98),
    ('D31_POWER', 98.2),
    ('D31_ENERGY', 8044.0),
    ('D31_CURRENT', 36.4)
)
INSERT INTO history.history_1s (time, tag_id, value)
SELECT NOW() - (INTERVAL '1 minute' * (ROW_NUMBER() OVER (ORDER BY s.code) % 5)),
       t.id,
       s.value
FROM samples s
JOIN scada.tag t ON t.code = s.code;

-- Extra samples for TB11/TB12 CURRENT + voltage tags (any matching codes)
INSERT INTO history.history_1s (time, tag_id, value)
SELECT NOW() - INTERVAL '2 minutes', t.id,
       CASE
         WHEN t.tag ILIKE '%voltage%' THEN 380 + (random() * 3)
         WHEN t.tag ILIKE '%current%' THEN 20 + (random() * 30)
         WHEN t.tag ILIKE '%powerfactor%' THEN 0.85 + (random() * 0.1)
         WHEN t.tag ILIKE '%frequency%' OR t.tag ILIKE 'freq%' THEN 49.9 + (random() * 0.2)
         WHEN t.tag = 'Power' THEN 80 + (random() * 60)
         WHEN t.tag = 'Energy' THEN 5000 + (random() * 2000)
         WHEN t.unit = '°C' THEN 40 + (random() * 25)
         WHEN t.unit = 'm' THEN 1 + (random() * 2)
         ELSE random() * 100
       END
FROM scada.tag t
JOIN scada.device d ON d.id = t.device_id
JOIN scada.plc p ON p.id = d.plc_id
JOIN scada.station s ON s.id = p.station_id
WHERE s.code IN ('TB11','TB12')
   OR d.id IN (1, 31);

COMMIT;

-- Quick verify
SELECT 'stations' AS k, COUNT(*)::text AS v FROM scada.station
UNION ALL
SELECT 'tags_electrical_like', COUNT(*)::text FROM scada.tag
  WHERE code ~ '(VOLTAGE_|CURRENT|POWER|FREQ|ENERGY|TEMP_|BEARING_|LEVEL_)'
     OR tag IN ('Current','VoltageRS','VoltageST','VoltageTR','PowerFactor','Frequency','Power','Energy');
