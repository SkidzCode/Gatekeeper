-- Change the delimiter to allow defining the stored procedure
DELIMITER //

-- Drop the procedure if it already exists
DROP PROCEDURE IF EXISTS CheckUsernameExists //

-- Create the stored procedure
CREATE PROCEDURE CheckUsernameExists (
    IN p_username VARCHAR(50),    -- Input parameter: Username to check
    OUT p_exists BOOLEAN           -- Output parameter: TRUE if exists, FALSE otherwise
)
BEGIN
    -- Declare a variable to hold the count of matching usernames
    DECLARE cnt INT;

    -- Count the number of users with the given username
    SELECT COUNT(*) INTO cnt
    FROM Users
    WHERE Username = p_username;

    -- Set the output parameter based on the count
    IF cnt > 0 THEN
        SET p_exists = TRUE;
    ELSE
        SET p_exists = FALSE;
    END IF;
END //

-- Revert the delimiter back to the default
DELIMITER ;