DROP PROCEDURE IF EXISTS GetSettingsByCategory;
DELIMITER //

CREATE PROCEDURE GetSettingsByCategory(IN p_Category VARCHAR(50))
BEGIN
    SELECT *
    FROM Settings
    WHERE Category = p_Category;
END //

DELIMITER ;
