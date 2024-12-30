-- Step 1: Create the database if it doesn't already exist
CREATE DATABASE IF NOT EXISTS hangfiredb
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_general_ci;

-- Step 2: Create a dedicated user for Hangfire
CREATE USER IF NOT EXISTS 'hangfireuser'@'%' IDENTIFIED BY 'StrongPassword123!';

-- Step 3: Grant permissions to the user
GRANT ALL PRIVILEGES ON hangfiredb.* TO 'hangfireuser'@'%';

-- Step 4: Apply changes
FLUSH PRIVILEGES;

-- Step 5: Verify the user and privileges (optional)
-- Run this to confirm that the user has the required permissions:
SHOW GRANTS FOR 'hangfireuser'@'%';
