DROP PROCEDURE IF EXISTS UpdateUser;
DELIMITER //

CREATE PROCEDURE UpdateUser(
    IN p_Id INT,
    IN p_Username VARCHAR(50),
    IN p_Email VARCHAR(100),
    IN p_FirstName VARCHAR(50),
    IN p_LastName VARCHAR(50),
    IN p_Phone VARCHAR(15)
)
BEGIN
    UPDATE Users
    SET 
        Username = IFNULL(p_Username, Username),
        Email = IFNULL(p_Email, Email),
        FirstName = IFNULL(p_FirstName, FirstName),
        LastName = IFNULL(p_LastName, LastName),
        Phone = IFNULL(p_Phone, Phone)
    WHERE Id = p_Id;
END //

DELIMITER ;