-- Reset + seed public.history_30m với Tag.Id THẬT (không invent id).
-- Chỉ các tag dùng màn báo cáo: Level (River/Discharge1), nhiệt Pump1–10, điện đồng hồ.
-- Chạy: psql -h localhost -U postgres -d scada_tlhn -f scripts/seed_history_30m_report.sql

BEGIN;

TRUNCATE TABLE public.history_30m;

WITH report_tags AS (
  -- Mức nước
  SELECT t."Id" AS tag_id, t."Code" AS tag_code, 'level'::text AS kind
  FROM public."Tag" t
  JOIN public."Device" d ON d."Id" = t."DeviceId"
  WHERE d."Code" = 'Level'
    AND upper(t."Code") IN ('RIVER', 'DISCHARGE1')

  UNION ALL

  -- Nhiệt độ bơm 1–10
  SELECT t."Id", t."Code", 'pump_temp'
  FROM public."Tag" t
  JOIN public."Device" d ON d."Id" = t."DeviceId"
  WHERE d."Code" ~ '^Pump([1-9]|10)$'
    AND upper(t."Code") IN (
      'FB_TEMP_PHASEA', 'FB_TEMP_PHASEB', 'FB_TEMP_PHASEC',
      'FB_TEMP_DEBEARING', 'FB_TEMP_NDEBEARING'
    )

  UNION ALL

  -- Đồng hồ điện (Metter*/Meter*): cột báo cáo điện
  SELECT t."Id", t."Code", 'meter'
  FROM public."Tag" t
  JOIN public."Device" d ON d."Id" = t."DeviceId"
  WHERE (
      d."DeviceType" = 'PowerMeter'
      OR d."Code" ILIKE '%Meter%'
      OR d."Code" ILIKE '%Metter%'
    )
    AND upper(replace(t."Code", '-', '_')) IN (
      'U12', 'U23', 'U31', 'I1', 'I2', 'I3', 'I_PH', 'TOTAL_KW', 'POWER_FACTOR', 'PF'
    )
),
slots AS (
  -- 3 ngày gần nhất (giờ VN +07), mỗi 30 phút
  SELECT generate_series(
    date_trunc('day', (now() AT TIME ZONE 'Asia/Ho_Chi_Minh') - interval '2 days')
      AT TIME ZONE 'Asia/Ho_Chi_Minh',
    date_trunc('hour', now()) + interval '30 minutes',
    interval '30 minutes'
  ) AS ts
)
INSERT INTO public.history_30m ("Time", "TagId", "Value")
SELECT
  s.ts,
  r.tag_id,
  round((
    CASE r.kind
      WHEN 'level' THEN
        CASE
          WHEN upper(r.tag_code) = 'RIVER' THEN 3.2
          ELSE 1.8
        END
        + 0.35 * sin(extract(epoch from s.ts) / 7200.0 + r.tag_id)
        + 0.12 * cos(extract(epoch from s.ts) / 3600.0)
      WHEN 'pump_temp' THEN
        CASE
          WHEN upper(r.tag_code) LIKE '%PHASEA%' THEN 42
          WHEN upper(r.tag_code) LIKE '%PHASEB%' THEN 45
          WHEN upper(r.tag_code) LIKE '%PHASEC%' THEN 48
          WHEN upper(r.tag_code) LIKE '%DEBEARING%' THEN 38
          ELSE 36
        END
        + 4.5 * sin(extract(epoch from s.ts) / 5400.0 + r.tag_id * 0.17)
        + 1.8 * cos(extract(epoch from s.ts) / 2700.0)
      ELSE
        CASE
          WHEN upper(r.tag_code) IN ('U12', 'U23', 'U31') THEN 380
          WHEN upper(r.tag_code) IN ('I1', 'I2', 'I3', 'I_PH') THEN 25
          WHEN upper(r.tag_code) = 'TOTAL_KW' THEN 120
          ELSE 0.92
        END
        + CASE
            WHEN upper(r.tag_code) IN ('U12', 'U23', 'U31') THEN
              8 * sin(extract(epoch from s.ts) / 4800.0 + r.tag_id)
            WHEN upper(r.tag_code) IN ('I1', 'I2', 'I3', 'I_PH') THEN
              6 * sin(extract(epoch from s.ts) / 3600.0 + r.tag_id)
            WHEN upper(r.tag_code) = 'TOTAL_KW' THEN
              15 * sin(extract(epoch from s.ts) / 4200.0)
            ELSE
              0.03 * sin(extract(epoch from s.ts) / 6000.0)
          END
    END
  )::numeric, 2)
FROM slots s
CROSS JOIN report_tags r
WHERE EXISTS (SELECT 1 FROM public."Tag" t WHERE t."Id" = r.tag_id);

COMMIT;

-- Kiểm tra
SELECT COUNT(*) AS total_rows,
       COUNT(DISTINCT "TagId") AS distinct_tags,
       MIN("Time") AS min_time,
       MAX("Time") AS max_time
FROM public.history_30m;

SELECT COUNT(*) AS orphan_rows
FROM public.history_30m h
WHERE NOT EXISTS (SELECT 1 FROM public."Tag" t WHERE t."Id" = h."TagId");

SELECT "TagId", COUNT(*) AS n
FROM public.history_30m
WHERE "TagId" IN (605, 606, 16, 17, 18)
GROUP BY 1
ORDER BY 1;
