DROP PROCEDURE IF EXISTS AddOrUpdateSetting;
DELIMITER //

CREATE PROCEDURE AddOrUpdateSetting (
    IN p_Id INT,
    IN p_ParentId INT,
    IN p_Name VARCHAR(100),
    IN p_Category VARCHAR(50),
    IN p_UserId INT,
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

    IF p_Id IS NULL THEN
        -- Perform an INSERT
        INSERT INTO Settings (
            ParentId,
            Name,
            Category,
            UserId,
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
            p_UserId,
            p_SettingValueType,
            p_DefaultSettingValue,
            p_SettingValue,
            p_CreatedBy,
            p_UpdatedBy
        );
    ELSE
        -- Perform an UPDATE
        UPDATE Settings
        SET
            ParentId = p_ParentId,
            Name = p_Name,
            Category = p_Category,
            UserId = p_UserId,
            SettingValueType = p_SettingValueType,
            DefaultSettingValue = p_DefaultSettingValue,
            SettingValue = p_SettingValue,
            UpdatedBy = p_UpdatedBy,
            UpdatedAt = CURRENT_TIMESTAMP
        WHERE Id = p_Id;
    END IF;

    COMMIT;

    -- Retrieve the Id of the inserted or updated setting
    SELECT *
    FROM Settings
    WHERE Id = COALESCE(p_Id, LAST_INSERT_ID());
END //

DELIMITER ;
