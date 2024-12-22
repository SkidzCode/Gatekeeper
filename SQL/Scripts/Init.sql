INSERT IGNORE INTO Roles (RoleName) VALUES ('Admin'), ('NewUser'), ('User');

-- Drop the MySQL user if it exists
DROP USER IF EXISTS 'api_user'@'%';

-- Create a new MySQL user with a secure password
CREATE USER 'api_user'@'%' IDENTIFIED BY 'SecurePassword!123';

-- Grant the `api_user` permission to execute stored procedures
GRANT EXECUTE ON UserManagement.* TO 'api_user'@'%';

-- Optionally grant SELECT permission on the `Users` table
GRANT SELECT ON UserManagement.Users TO 'api_user'@'%';

-- Apply the changes to privileges
FLUSH PRIVILEGES;