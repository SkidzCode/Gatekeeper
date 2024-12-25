DROP PROCEDURE IF EXISTS GetUser;

-- Create a stored procedure to retrieve user profile information
DELIMITER //
CREATE PROCEDURE GetUser(
    IN p_UserId INT
)
BEGIN
    SELECT Id,
        Salt,
        Password,
        FirstName,
        LastName,
        Email,
        Username,
        Phone,
		IsActive,
		CreatedAt,
		UpdatedAt
    FROM Users
    WHERE Id = p_UserId;
END //
DELIMITER ;
