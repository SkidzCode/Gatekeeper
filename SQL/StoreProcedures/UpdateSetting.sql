DROP PROCEDURE IF EXISTS UpdateSetting;
DELIMITER //

CREATE PROCEDURE UpdateSetting (
    IN p_Id INT,
    IN p_ParentId INT,
    IN p_Name VARCHAR(100),
    IN p_Category VARCHAR(50),
    IN p_SettingValueType ENUM('string', 'integer', 'boolean', 'float', 'json'),
    IN p_DefaultSettingValue TEXT,
    IN p_SettingValue TEXT,
    IN p_UpdatedBy INT
)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        -- Rollback in case of error
        ROLLBACK;
        SELECT 'An error occurred while updating the setting.' AS ErrorMessage;
    END;

    START TRANSACTION;

    UPDATE Settings
    SET
        ParentId = p_ParentId,
        Name = p_Name,
        Category = p_Category,
        SettingValueType = p_SettingValueType,
        DefaultSettingValue = p_DefaultSettingValue,
        SettingValue = p_SettingValue,
        UpdatedBy = p_UpdatedBy,
        UpdatedAt = CURRENT_TIMESTAMP
    WHERE Id = p_Id;

    COMMIT;

    SELECT ROW_COUNT() AS RowsAffected;
END //

DELIMITER ;
