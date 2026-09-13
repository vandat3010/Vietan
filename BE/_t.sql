SELECT d."Name", d."DeviceType", t."Code", t."Tag", t."DataType", t."EnableRealtime"
FROM public."Tag" t
JOIN public."Device" d ON d."Id"=t."DeviceId"
WHERE d."DeviceType" ILIKE '%pump%' OR d."Name" ILIKE '%bom%' OR d."Name" ILIKE '%pump%'
ORDER BY d."Id", t."Id"
LIMIT 40;
SELECT COUNT(*) FILTER (WHERE m."IsRealtime") AS realtime_maps FROM app.tag_screen_mapping m;
SELECT COUNT(*) FROM app.tag_screen_mapping;
