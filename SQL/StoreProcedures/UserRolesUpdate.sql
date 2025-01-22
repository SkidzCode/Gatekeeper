DROP PROCEDURE IF EXISTS UserRolesUpdate;
DELIMITER $$

CREATE PROCEDURE UserRolesUpdate(
    IN pUserId INT,
    IN pRoleNames VARCHAR(1000)  -- comma-separated role names, e.g. 'Admin,SuperUser,PowerUser'
)
BEGIN
    DECLARE currentRoleName VARCHAR(50);
    DECLARE commaPos INT;
    DECLARE roleId INT;

    -- 1) Remove roles that are not in the new list
    DELETE ur
    FROM UserRoles ur
    JOIN Roles r ON ur.RoleId = r.Id
    WHERE ur.UserId = pUserId
      AND (FIND_IN_SET(r.RoleName, pRoleNames) = 0 OR pRoleNames = '');

    -- 2) Loop over the role names string
    WHILE LENGTH(pRoleNames) > 0 DO

        -- Grab the next role name
        SET commaPos = INSTR(pRoleNames, ',');
        IF commaPos > 0 THEN
            SET currentRoleName = SUBSTRING(pRoleNames, 1, commaPos - 1);
            SET pRoleNames = SUBSTRING(pRoleNames, commaPos + 1);
        ELSE
            SET currentRoleName = pRoleNames;
            SET pRoleNames = '';
        END IF;

        -- Trim spaces just in case
        SET currentRoleName = TRIM(currentRoleName);

        IF LENGTH(currentRoleName) > 0 THEN
            -- Look up the role's ID
            SELECT Id INTO roleId
            FROM Roles
            WHERE RoleName = currentRoleName
            LIMIT 1;

            -- If the role exists, insert (ignore duplicates)
            IF roleId IS NOT NULL THEN
                INSERT IGNORE INTO UserRoles (UserId, RoleId)
                VALUES (pUserId, roleId);
            END IF;
        END IF;

    END WHILE;
END$$

DELIMITER ;
