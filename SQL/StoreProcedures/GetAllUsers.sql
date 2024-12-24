DROP PROCEDURE IF EXISTS GetAllUsers;
DELIMITER //

CREATE PROCEDURE GetAllUsers()
BEGIN
    SELECT Id,Username,Email,Password,Salt,FirstName,LastName,Phone,IsActive,CreatedAt,UpdatedAt 
    FROM Users;
END //

DELIMITER ;
