DROP PROCEDURE IF EXISTS GetUserProfile;

-- Create a stored procedure to retrieve user profile information
DELIMITER //
CREATE PROCEDURE GetUserProfile(
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
        Phone
    FROM Users
    WHERE Id = p_UserId AND IsActive = 1;
	
	-- Second result set: User roles
    SELECT r.RoleName
    FROM UserRoles ur
    JOIN Roles r ON ur.RoleId = r.Id
    JOIN Users u ON ur.UserId = u.Id
    WHERE Id = p_UserId AND u.IsActive = 1;
END //
DELIMITER ;

-- Drop the procedure if it exists
DROP PROCEDURE IF EXISTS GetUserProfileByIdentifier;

-- Create a stored procedure to retrieve user profile information
DELIMITER //
CREATE PROCEDURE GetUserProfileByIdentifier(
    IN p_Identifier VARCHAR(120)
)
BEGIN
    -- First result set: User profile information
    SELECT Id,
        Salt,
        Password,
        FirstName,
        LastName,
        Email,
        Username,
        Phone
    FROM Users
    WHERE (Username = p_Identifier OR Email = p_Identifier) AND IsActive = 1;

    -- Second result set: User roles
    SELECT r.RoleName
    FROM UserRoles ur
    JOIN Roles r ON ur.RoleId = r.Id
    JOIN Users u ON ur.UserId = u.Id
    WHERE (u.Username = p_Identifier OR u.Email = p_Identifier) AND u.IsActive = 1;
END //
DELIMITER ;
