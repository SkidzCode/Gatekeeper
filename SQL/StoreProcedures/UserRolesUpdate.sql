DROP PROCEDURE IF EXISTS UserRolesUpdate;
DELIMITER $$

CREATE PROCEDURE UserRolesUpdate(
    IN pUserId INT,
    IN pRoleIds VARCHAR(1000)
)
BEGIN
    -- 1) Remove roles that are not in the new list.
    DELETE FROM UserRoles
    WHERE UserId = pUserId
      AND (FIND_IN_SET(RoleId, pRoleIds) = 0 OR pRoleIds = '');

    -- 2) Loop through the pRoleIds string and insert if missing.
    WHILE (LENGTH(pRoleIds) > 0) DO

        INSERT IGNORE INTO UserRoles (UserId, RoleId)
        VALUES (
            pUserId, 
            CAST(SUBSTRING_INDEX(pRoleIds, ',', 1) AS UNSIGNED)
        );

        -- Remove the first RoleId from the comma-separated list.
        SET pRoleIds = SUBSTRING(
            pRoleIds, 
            INSTR(pRoleIds, ',') + 1
        );

        -- If there is no comma left, process the last value and exit loop.
        IF (INSTR(pRoleIds, ',') = 0) THEN
            IF (LENGTH(pRoleIds) > 0) THEN
                INSERT IGNORE INTO UserRoles (UserId, RoleId)
                VALUES (
                    pUserId, 
                    CAST(pRoleIds AS UNSIGNED)
                );
            END IF;

            SET pRoleIds = '';
        END IF;
    END WHILE;
END$$

DELIMITER ;
