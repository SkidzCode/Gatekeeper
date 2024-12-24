DROP PROCEDURE IF EXISTS UpdateRole;
DELIMITER //

CREATE PROCEDURE UpdateRole(
    IN p_Id INT,
    IN p_RoleName VARCHAR(50)
)
BEGIN
    UPDATE Roles
    SET 
        RoleName = IFNULL(p_RoleName, RoleName)
    WHERE Id = p_Id;
END //

DELIMITER ;
