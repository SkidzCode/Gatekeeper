-- Drop the stored procedure if it exists
DROP PROCEDURE IF EXISTS GetUserDetailsForLogin;

-- Create the stored procedure to retrieve user details for login
DELIMITER //
CREATE PROCEDURE GetUserDetailsForLogin(
    IN p_Identifier VARCHAR(100) -- Input parameter for email OR username
)
BEGIN
    -- Retrieve user details (Id, Salt, Hashed Password) by email or username
    SELECT
        Id,
        Salt,
        Password,
        FirstName,
        LastName,
        Email,
        Username,
        Phone
    FROM Users
    WHERE (Username = p_Identifier OR Email = p_Identifier) AND IsActive = 1;
END //
DELIMITER ;