-- Seed demo alarm_history: station → device → tag (Tag.Id thật).
-- Chạy: psql -h localhost -U postgres -d scada_tlhn -f scripts/seed_alarm_history_demo.sql

BEGIN;

TRUNCATE TABLE public.alarm_history RESTART IDENTITY;

WITH fault_tags AS (
  SELECT
    t."Id" AS tag_id,
    t."Code" AS tag_code,
    d."Id" AS device_id,
    d."Name" AS device_name,
    p."Id" AS plc_id,
    p."StationId" AS station_id,
    row_number() OVER (ORDER BY d."Code", t."Id") AS rn
  FROM public."Tag" t
  JOIN public."Device" d ON d."Id" = t."DeviceId"
  JOIN public."PLC" p ON p."Id" = d."PlcId"
  WHERE p."StationId" = 1
    AND d."Code" ~ '^Pump([1-9]|10)$'
    AND upper(t."Code") IN (
      'FB_FAULT', 'FB_FAULT_V', 'FB_FAULT_SS', 'FB_FAULT_TEMP', 'FB_FAULT_CURENT'
    )
),
slots AS (
  SELECT generate_series(0, 89) AS i
)
INSERT INTO public.alarm_history (
  "StationId", "PlcId", "DeviceId", "TagId",
  "TagEventConfigId", "EventTypeId", "TriggerTypeId",
  "DeviceName", "TagName", "Description", "TroubleshootingGuide",
  "Type", "IsAcknowledged", "DurationSeconds",
  "StartTime", "EndTime", "CreatedAt"
)
SELECT
  f.station_id,
  f.plc_id,
  f.device_id,
  f.tag_id,
  NULL,
  CASE
    WHEN s.i % 10 = 0 THEN 2   -- WARNING
    WHEN s.i % 7 = 0 THEN 1    -- ALARM
    ELSE 7                     -- FAULT
  END,
  NULL,
  f.device_name,
  'Pump_' || regexp_replace(f.device_name, '[^0-9]', '', 'g') || '_' || f.tag_code,
  CASE
    WHEN upper(f.tag_code) LIKE '%TEMP%' THEN 'Lỗi nhiệt độ ' || regexp_replace(f.device_name, '[^0-9]', '', 'g')
    WHEN upper(f.tag_code) LIKE '%CURENT%' OR upper(f.tag_code) LIKE '%CURRENT%' THEN 'Lỗi dòng điện ' || f.device_name
    WHEN upper(f.tag_code) LIKE '%_V' THEN 'Lỗi điện áp ' || f.device_name
    WHEN upper(f.tag_code) LIKE '%_SS%' THEN 'Lỗi cảm biến ' || f.device_name
    ELSE 'Lỗi ' || f.device_name
  END,
  NULL,
  CASE
    WHEN s.i % 10 = 0 THEN 'WARNING'
    WHEN s.i % 7 = 0 THEN 'ALARM'
    ELSE 'ERROR'
  END,
  (s.i % 3 = 0),
  EXTRACT(EPOCH FROM (interval '2 hours' + (s.i % 5) * interval '30 minutes')),
  (
    date_trunc('day', (now() AT TIME ZONE 'Asia/Ho_Chi_Minh') - ((s.i % 25) || ' days')::interval)
    + time '09:30:01'
    + (s.i % 8) * interval '1 hour'
  ) AT TIME ZONE 'Asia/Ho_Chi_Minh',
  (
    date_trunc('day', (now() AT TIME ZONE 'Asia/Ho_Chi_Minh') - ((s.i % 25) || ' days')::interval)
    + time '17:31:01'
    + (s.i % 3) * interval '20 minutes'
  ) AT TIME ZONE 'Asia/Ho_Chi_Minh',
  now()
FROM slots s
JOIN fault_tags f ON f.rn = (s.i % (SELECT COUNT(*) FROM fault_tags)) + 1;

-- value-change: START/STOP
WITH start_stop AS (
  SELECT
    t."Id" AS tag_id,
    t."Code" AS tag_code,
    d."Id" AS device_id,
    d."Name" AS device_name,
    p."Id" AS plc_id,
    p."StationId" AS station_id,
    row_number() OVER (ORDER BY d."Code") AS rn
  FROM public."Tag" t
  JOIN public."Device" d ON d."Id" = t."DeviceId"
  JOIN public."PLC" p ON p."Id" = d."PlcId"
  WHERE p."StationId" = 1
    AND d."Code" ~ '^Pump([1-9]|10)$'
    AND upper(t."Code") IN ('FB_RUN', 'CTRL_RUN_PUMP')
),
slots AS (
  SELECT generate_series(0, 29) AS i
)
INSERT INTO public.alarm_history (
  "StationId", "PlcId", "DeviceId", "TagId",
  "EventTypeId", "DeviceName", "TagName", "Description",
  "Type", "IsAcknowledged", "DurationSeconds",
  "StartTime", "EndTime", "CreatedAt"
)
SELECT
  f.station_id, f.plc_id, f.device_id, f.tag_id,
  CASE WHEN s.i % 2 = 0 THEN 4 ELSE 5 END,
  f.device_name,
  f.tag_code,
  CASE WHEN s.i % 2 = 0 THEN 'Khởi động ' || f.device_name ELSE 'Dừng ' || f.device_name END,
  CASE WHEN s.i % 2 = 0 THEN 'START' ELSE 'STOP' END,
  true,
  60,
  (
    date_trunc('day', (now() AT TIME ZONE 'Asia/Ho_Chi_Minh') - ((s.i % 10) || ' days')::interval)
    + time '08:00:00' + (s.i % 6) * interval '90 minutes'
  ) AT TIME ZONE 'Asia/Ho_Chi_Minh',
  (
    date_trunc('day', (now() AT TIME ZONE 'Asia/Ho_Chi_Minh') - ((s.i % 10) || ' days')::interval)
    + time '08:01:00' + (s.i % 6) * interval '90 minutes'
  ) AT TIME ZONE 'Asia/Ho_Chi_Minh',
  now()
FROM slots s
JOIN start_stop f ON f.rn = (s.i % (SELECT COUNT(*) FROM start_stop)) + 1;

-- system / EVENT
INSERT INTO public.alarm_history (
  "StationId", "PlcId", "DeviceId", "TagId",
  "EventTypeId", "DeviceName", "TagName", "Description",
  "Type", "IsAcknowledged", "DurationSeconds",
  "StartTime", "EndTime", "CreatedAt"
)
SELECT
  1, 1, NULL, NULL,
  6,
  NULL,
  NULL,
  'Sự kiện hệ thống #' || g,
  CASE WHEN g % 2 = 0 THEN 'SYSTEM' ELSE 'Communication' END,
  true,
  NULL,
  (
    date_trunc('day', (now() AT TIME ZONE 'Asia/Ho_Chi_Minh') - ((g % 7) || ' days')::interval)
    + time '12:00:00' + g * interval '15 minutes'
  ) AT TIME ZONE 'Asia/Ho_Chi_Minh',
  NULL,
  now()
FROM generate_series(1, 20) g;

COMMIT;

SELECT "Type", COUNT(*) FROM public.alarm_history GROUP BY 1 ORDER BY 1;
SELECT COUNT(*) AS total FROM public.alarm_history;
