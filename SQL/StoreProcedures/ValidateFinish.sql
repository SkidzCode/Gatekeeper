DROP PROCEDURE IF EXISTS ValidateFinish;
DELIMITER //

CREATE PROCEDURE ValidateFinish(
	IN p_UserId INT,
	IN p_Id VARCHAR(36)
)
BEGIN
	DECLARE roleIdGroup2 INT;
    DECLARE roleIdGroup3 INT;
    
	UPDATE Verification SET Complete = 1 WHERE Id = p_Id;

    -- Get RoleId for 'New Users' (Group 2)
    SELECT Id INTO roleIdGroup2 FROM Roles WHERE RoleName = 'NewUser' LIMIT 1;

    -- Get RoleId for 'Users' (Group 3)
    SELECT Id INTO roleIdGroup3 FROM Roles WHERE RoleName = 'User' LIMIT 1;

    -- Ensure both RoleIds are valid before proceeding
    IF roleIdGroup2 IS NOT NULL AND roleIdGroup3 IS NOT NULL THEN
        -- Delete user from Group 2 (New Users)
        DELETE FROM UserRoles WHERE UserId = p_UserId AND RoleId = roleIdGroup2;

        -- Add user to Group 3 (Users) if not already present
        INSERT IGNORE INTO UserRoles(UserId, RoleId) VALUES (p_UserId, roleIdGroup3);
    END IF;
END //
DELIMITER ;