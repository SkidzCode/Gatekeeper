DROP PROCEDURE IF EXISTS PasswordChange;
DELIMITER //

CREATE PROCEDURE PasswordChange (
    IN p_UserId INT,
    IN p_HashedPassword VARCHAR(255),
    IN p_Salt VARCHAR(255)
)
BEGIN
    -- Update the user's password and salt in the Users table
    UPDATE Users
    SET 
        Password = p_HashedPassword,
        Salt = p_Salt,
        UpdatedAt = CURRENT_TIMESTAMP
    WHERE Id = p_UserId;
END //
DELIMITER ;
