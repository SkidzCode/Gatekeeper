DROP PROCEDURE IF EXISTS AddOrUpdateSetting;
DELIMITER //

CREATE PROCEDURE AddOrUpdateSetting (
    IN p_ParentId INT,
    IN p_Name VARCHAR(100),
    IN p_Category VARCHAR(50),
    IN p_SettingValueType ENUM('string', 'integer', 'boolean', 'float', 'json'),
    IN p_DefaultSettingValue TEXT,
    IN p_SettingValue TEXT,
    IN p_CreatedBy INT,
    IN p_UpdatedBy INT
)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        -- Rollback in case of error
        ROLLBACK;
        SELECT 'An error occurred while adding or updating the setting.' AS ErrorMessage;
    END;

    START TRANSACTION;

    INSERT INTO Settings (
        ParentId,
        Name,
        Category,
        SettingValueType,
        DefaultSettingValue,
        SettingValue,
        CreatedBy,
        UpdatedBy
    )
    VALUES (
        p_ParentId,
        p_Name,
        p_Category,
        p_SettingValueType,
        p_DefaultSettingValue,
        p_SettingValue,
        p_CreatedBy,
        p_UpdatedBy
    )
    ON DUPLICATE KEY UPDATE
        ParentId = VALUES(ParentId),
        Category = VALUES(Category),
        SettingValueType = VALUES(SettingValueType),
        DefaultSettingValue = VALUES(DefaultSettingValue),
        SettingValue = VALUES(SettingValue),
        UpdatedBy = VALUES(UpdatedBy),
        UpdatedAt = CURRENT_TIMESTAMP;

    COMMIT;

    -- Retrieve the Id of the inserted or updated setting
    SELECT Id AS SettingId, 
           ROW_COUNT() AS RowsAffected
    FROM Settings
    WHERE Name = p_Name;
END //

DELIMITER ;
