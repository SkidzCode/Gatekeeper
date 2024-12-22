DROP PROCEDURE IF EXISTS AssignRoleToUser;
DELIMITER //
CREATE PROCEDURE AssignRoleToUser(IN p_UserId INT, IN p_RoleName VARCHAR(50))
BEGIN
    DECLARE roleId INT;
    SELECT Id INTO roleId FROM Roles WHERE RoleName = p_RoleName LIMIT 1;
    IF roleId IS NOT NULL THEN
        INSERT IGNORE INTO UserRoles(UserId, RoleId) VALUES (p_UserId, roleId);
    END IF;
END //
DELIMITER ;