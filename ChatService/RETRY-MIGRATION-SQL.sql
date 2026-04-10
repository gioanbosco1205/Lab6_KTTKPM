-- ============================================
-- MIGRATION: AddRetryMechanismToMessages
-- Purpose: Add retry mechanism with max retry = 5
-- ============================================

-- Add RetryCount column (NOT NULL with default 0)
ALTER TABLE "Messages" 
ADD COLUMN "RetryCount" integer NOT NULL DEFAULT 0;

-- Add LastRetryAt column (NULL)
ALTER TABLE "Messages" 
ADD COLUMN "LastRetryAt" timestamp with time zone NULL;

-- Create index on RetryCount for better query performance
CREATE INDEX "IX_Messages_RetryCount" ON "Messages" ("RetryCount");

-- Create composite index for retry queries
CREATE INDEX "IX_Messages_ProcessedAt_RetryCount" ON "Messages" ("ProcessedAt", "RetryCount");

-- Update existing records to have RetryCount = 0 (if any exist)
UPDATE "Messages" 
SET "RetryCount" = 0 
WHERE "RetryCount" IS NULL;

-- ============================================
-- VERIFICATION QUERIES
-- ============================================

-- Check if columns were added
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns 
WHERE table_name = 'Messages' 
  AND column_name IN ('RetryCount', 'LastRetryAt');

-- Check if indexes were created
SELECT indexname, indexdef 
FROM pg_indexes 
WHERE tablename = 'Messages' 
  AND indexname IN ('IX_Messages_RetryCount', 'IX_Messages_ProcessedAt_RetryCount');

-- Check sample data
SELECT "Id", "Type", "RetryCount", "LastRetryAt", "CreatedAt", "ProcessedAt" 
FROM "Messages" 
LIMIT 5;

-- ============================================
-- MONITORING QUERIES
-- ============================================

-- Messages đang retry
SELECT "Id", "Type", "RetryCount", "LastRetryAt", "CreatedAt"
FROM "Messages"
WHERE "ProcessedAt" IS NULL
  AND "RetryCount" > 0
ORDER BY "RetryCount" DESC, "CreatedAt" ASC;

-- Messages đã vượt quá max retry (dead letter)
SELECT "Id", "Type", "RetryCount", "LastRetryAt", "ProcessedAt"
FROM "Messages"
WHERE "ProcessedAt" IS NOT NULL
  AND "RetryCount" >= 5
ORDER BY "ProcessedAt" DESC;

-- Statistics
SELECT 
    COUNT(*) as TotalMessages,
    SUM(CASE WHEN "ProcessedAt" IS NULL AND "RetryCount" = 0 THEN 1 ELSE 0 END) as Fresh,
    SUM(CASE WHEN "ProcessedAt" IS NULL AND "RetryCount" BETWEEN 1 AND 4 THEN 1 ELSE 0 END) as Retrying,
    SUM(CASE WHEN "RetryCount" >= 5 THEN 1 ELSE 0 END) as DeadLetter,
    SUM(CASE WHEN "ProcessedAt" IS NOT NULL AND "RetryCount" < 5 THEN 1 ELSE 0 END) as Successful
FROM "Messages";

-- Average retry count for failed messages
SELECT 
    AVG("RetryCount") as AvgRetryCount,
    MAX("RetryCount") as MaxRetryCount,
    MIN("RetryCount") as MinRetryCount
FROM "Messages"
WHERE "RetryCount" > 0;

-- ============================================
-- ROLLBACK (if needed)
-- ============================================

-- DROP INDEX "IX_Messages_ProcessedAt_RetryCount";
-- DROP INDEX "IX_Messages_RetryCount";
-- ALTER TABLE "Messages" DROP COLUMN "LastRetryAt";
-- ALTER TABLE "Messages" DROP COLUMN "RetryCount";
