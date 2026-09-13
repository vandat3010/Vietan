-- Demo seed: ~15 phút history_1s cho đồ thị nhiệt (Pump1) + dòng (Metter1).
-- Chạy: psql -h localhost -U postgres -d scada_tlhn -f scripts/seed_chart_demo_history.sql

DELETE FROM public.history_1s WHERE "TagId" IN (14,15,16,17,18,27,28,29);

INSERT INTO public.history_1s ("Time", "TagId", "Value")
SELECT
  gs,
  tag_id,
  round((base
    + 6 * sin(extract(epoch from gs) / 45.0 + tag_id)
    + 3 * cos(extract(epoch from gs) / 20.0 + tag_id * 0.7)
  )::numeric, 2)
FROM generate_series(now() - interval '15 minutes', now(), interval '5 seconds') AS gs
CROSS JOIN (
  VALUES
    (14::bigint, 22.0::float8),
    (15::bigint, 28.0::float8),
    (16::bigint, 8.0::float8),
    (17::bigint, 10.0::float8),
    (18::bigint, 13.0::float8)
) AS t(tag_id, base);

INSERT INTO public.history_1s ("Time", "TagId", "Value")
SELECT
  gs,
  m."Id",
  round((
    CASE
      WHEN upper(m."Code") LIKE '%I1%' OR upper(m."Code") = 'IA' THEN 22
      WHEN upper(m."Code") LIKE '%I2%' OR upper(m."Code") = 'IB' THEN 28
      ELSE 18
    END
    + 8 * sin(extract(epoch from gs) / 40.0 + m."Id")
    + 4 * cos(extract(epoch from gs) / 18.0)
  )::numeric, 2)
FROM generate_series(now() - interval '15 minutes', now(), interval '5 seconds') gs
CROSS JOIN (
  SELECT t."Id", t."Code"
  FROM public."Tag" t
  WHERE t."DeviceId" = 14
    AND (
      upper(t."Code") IN ('I1','I2','I3','IA','IB','IC')
      OR t."Code" ILIKE '%I1%' OR t."Code" ILIKE '%I2%' OR t."Code" ILIKE '%I3%'
    )
) m;

SELECT "TagId", COUNT(*) AS n FROM public.history_1s GROUP BY 1 ORDER BY 1;
