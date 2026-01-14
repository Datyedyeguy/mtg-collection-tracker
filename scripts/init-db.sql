-- Initial database setup script
-- This runs automatically when the PostgreSQL container is first created
-- It will NOT run again if the volume already has data

-- Enable UUID generation extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Log that initialization completed
DO $$
BEGIN
    RAISE NOTICE 'MTG Collection Tracker database initialized successfully!';
END $$;
