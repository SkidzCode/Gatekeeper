DROP PROCEDURE IF EXISTS GetUserRoles;
DELIMITER //
CREATE PROCEDURE GetUserRoles(IN p_UserId INT)
BEGIN
    SELECT r.RoleName
    FROM Roles r
    INNER JOIN UserRoles ur ON r.Id = ur.RoleId
    WHERE ur.UserId = p_UserId;
END //
DELIMITER ;