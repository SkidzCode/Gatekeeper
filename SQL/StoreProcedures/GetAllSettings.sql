DROP PROCEDURE IF EXISTS GetAllSettings;
DELIMITER //

CREATE PROCEDURE GetAllSettings(IN p_UserId INT)
BEGIN
    SELECT 
        Id,
        ParentId,
        UserId,
        Name,
        Category,
        SettingValueType,
        DefaultSettingValue,
        IFNULL(SettingValue, DefaultSettingValue) AS SettingValue,
        CreatedBy,
        UpdatedBy,
        CreatedAt,
        UpdatedAt
    FROM Settings
    WHERE UserId = p_UserId OR UserId IS NULL;
END //

DELIMITER ;
