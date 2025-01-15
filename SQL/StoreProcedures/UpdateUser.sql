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


DROP PROCEDURE IF EXISTS UpdateUserPic;
DELIMITER //

CREATE PROCEDURE UpdateUserPic(
    IN p_Id INT,
    IN p_FirstName VARCHAR(50),
    IN p_LastName VARCHAR(50),
    IN p_Email VARCHAR(100),
    IN p_Username VARCHAR(50),
    IN p_Phone VARCHAR(15),
    IN p_ProfilePicture LONGBLOB
)
BEGIN
    -- All DECLAREs must come first
    DECLARE v_AssetTypeId INT;

    -- 1) Update core user data
    UPDATE Users
    SET 
        FirstName = p_FirstName,
        LastName  = p_LastName,
        Email     = p_Email,
        Username  = p_Username,
        Phone     = p_Phone,
        UpdatedAt = NOW()
    WHERE Id = p_Id;

    -- 2) Get the AssetType Id for "ProfilePicture"
    SELECT Id 
      INTO v_AssetTypeId
      FROM AssetTypes
     WHERE Name = 'ProfilePicture'
     LIMIT 1;

    -- 3) If p_ProfilePicture is NOT NULL, update or insert into Assets
    IF p_ProfilePicture IS NOT NULL THEN
        IF EXISTS (
            SELECT 1 
              FROM Assets 
             WHERE UserId = p_Id 
               AND AssetType = v_AssetTypeId
        ) THEN
            UPDATE Assets
               SET Asset     = p_ProfilePicture,
                   UpdatedAt = NOW()
             WHERE UserId    = p_Id
               AND AssetType = v_AssetTypeId;
        ELSE
            INSERT INTO Assets (UserId, AssetType, Asset)
            VALUES (p_Id, v_AssetTypeId, p_ProfilePicture);
        END IF;
    END IF;
END //

DELIMITER ;

