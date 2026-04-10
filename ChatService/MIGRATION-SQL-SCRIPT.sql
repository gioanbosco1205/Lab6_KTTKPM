-- ============================================
-- MIGRATION: AddCreatedAtProcessedAtToMessages
-- Generated: 2026-04-10
-- ============================================

-- Add CreatedAt column (NOT NULL with default)
ALTER TABLE "Messages" 
ADD COLUMN "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC');

-- Add ProcessedAt column (NULL)
ALTER TABLE "Messages" 
ADD COLUMN "ProcessedAt" timestamp with time zone NULL;

-- Create index on CreatedAt for better query performance
CREATE INDEX "IX_Messages_CreatedAt" ON "Messages" ("CreatedAt");

-- Create index on ProcessedAt for better query performance
CREATE INDEX "IX_Messages_ProcessedAt" ON "Messages" ("ProcessedAt");

-- Update existing records to have CreatedAt (if any exist)
UPDATE "Messages" 
SET "CreatedAt" = NOW() AT TIME ZONE 'UTC' 
WHERE "CreatedAt" = '0001-01-01 00:00:00+00';

-- ============================================
-- VERIFICATION QUERIES
-- ============================================

-- Check if columns were added
SELECT column_name, data_type, is_nullable 
FROM information_schema.columns 
WHERE table_name = 'Messages' 
  AND column_name IN ('CreatedAt', 'ProcessedAt');

-- Check if indexes were created
SELECT indexname, indexdef 
FROM pg_indexes 
WHERE tablename = 'Messages' 
  AND indexname IN ('IX_Messages_CreatedAt', 'IX_Messages_ProcessedAt');

-- Check sample data
SELECT "Id", "Type", "CreatedAt", "ProcessedAt" 
FROM "Messages" 
LIMIT 5;

-- ============================================
-- ROLLBACK (if needed)
-- ============================================

-- DROP INDEX "IX_Messages_ProcessedAt";
-- DROP INDEX "IX_Messages_CreatedAt";
-- ALTER TABLE "Messages" DROP COLUMN "ProcessedAt";
-- ALTER TABLE "Messages" DROP COLUMN "CreatedAt";
