-- Drop the procedure if it exists
DROP PROCEDURE IF EXISTS AddUser;

-- Create a stored procedure to add a new user
DELIMITER //
CREATE PROCEDURE AddUser(
    IN p_FirstName VARCHAR(50),
    IN p_LastName VARCHAR(50),
    IN p_Email VARCHAR(100),
    IN p_Username VARCHAR(50),
    IN p_Password VARCHAR(255),
    IN p_Salt VARCHAR(255),
    IN p_Phone VARCHAR(15),
    OUT p_ResultCode INT, -- Output parameter for result code
    OUT last_id INT
)
BEGIN
    -- Initialize result code (0 = success, 1 = email exists, 2 = username exists, 3 = both exist)
    SET p_ResultCode = 0;

    -- Check if email already exists
    IF EXISTS (SELECT 1 FROM Users WHERE Email = p_Email) THEN
        SET p_ResultCode = p_ResultCode + 1; -- Add 1 to the code
    END IF;

    -- Check if username already exists
    IF EXISTS (SELECT 1 FROM Users WHERE Username = p_Username) THEN
        SET p_ResultCode = p_ResultCode + 2; -- Add 2 to the code
    END IF;

    -- Only insert if neither email nor username exists
	IF p_ResultCode = 0 THEN
        INSERT INTO Users (FirstName, LastName, Email, Username, Password, Salt, Phone)
        VALUES (p_FirstName, p_LastName, p_Email, p_Username, p_Password, p_Salt, p_Phone);

        SET last_id = LAST_INSERT_ID(); -- Assign last insert ID to output parameter
    ELSE
        SET last_id = NULL;
    END IF;
END //
DELIMITER ;