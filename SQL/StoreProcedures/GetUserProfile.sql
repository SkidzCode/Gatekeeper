-- Drop the procedure if it exists
DROP PROCEDURE IF EXISTS GetUserProfile;

DELIMITER //
CREATE PROCEDURE GetUserProfile(
    IN p_UserId INT
)
BEGIN
    -- First result set: User profile information including profile picture from Assets
    SELECT 
        u.Id,
        u.Salt,
        u.Password,
        u.FirstName,
        u.LastName,
        u.Email,
        u.Username,
        u.Phone,
        a.Asset AS ProfilePicture
    FROM Users u
    LEFT JOIN Assets a
        ON a.UserId = u.Id
        AND a.AssetType = (
            SELECT Id FROM AssetTypes WHERE Name = 'ProfilePicture' LIMIT 1
        )
    WHERE u.Id = p_UserId
      AND u.IsActive = 1;

    -- Second result set: User roles
    SELECT r.RoleName
    FROM UserRoles ur
    JOIN Roles r ON ur.RoleId = r.Id
    JOIN Users u ON ur.UserId = u.Id
    WHERE ur.UserId = p_UserId
      AND u.IsActive = 1;
END //
DELIMITER ;

-- Drop the procedure if it exists
DROP PROCEDURE IF EXISTS GetUserProfileByIdentifier;

DELIMITER //
CREATE PROCEDURE GetUserProfileByIdentifier(
    IN p_Identifier VARCHAR(120)
)
BEGIN
    -- First result set: User profile information including profile picture from Assets
    SELECT 
        u.Id,
        u.Salt,
        u.Password,
        u.FirstName,
        u.LastName,
        u.Email,
        u.Username,
        u.Phone,
        a.Asset AS ProfilePicture
    FROM Users u
    LEFT JOIN Assets a
        ON a.UserId = u.Id
        AND a.AssetType = (
            SELECT Id FROM AssetTypes WHERE Name = 'ProfilePicture' LIMIT 1
        )
    WHERE (u.Username = p_Identifier OR u.Email = p_Identifier)
      AND u.IsActive = 1;

    -- Second result set: User roles
    SELECT r.RoleName
    FROM UserRoles ur
    JOIN Roles r ON ur.RoleId = r.Id
    JOIN Users u ON ur.UserId = u.Id
    WHERE (u.Username = p_Identifier OR u.Email = p_Identifier)
      AND u.IsActive = 1;
END //
DELIMITER ;
