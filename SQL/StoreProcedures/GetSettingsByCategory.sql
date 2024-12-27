DROP PROCEDURE IF EXISTS GetSettingsByCategory;
DELIMITER //

CREATE PROCEDURE GetSettingsByCategory(
	IN p_Category VARCHAR(50),
	IN p_UserId INT
)
BEGIN
    SELECT *
    FROM Settings
    WHERE Category = p_Category AND 
	(UserId = p_UserId OR UserId IS NULL);
END //

DELIMITER ;
