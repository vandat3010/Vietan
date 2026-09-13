SELECT table_schema, table_name FROM information_schema.tables WHERE table_name ILIKE '%alarm%' OR table_name ILIKE '%event%';
