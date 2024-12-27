DROP PROCEDURE IF EXISTS AddSetting;
DELIMITER //

CREATE PROCEDURE AddSetting (
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
        SELECT 'An error occurred while adding the setting.' AS ErrorMessage;
    END;

    START TRANSACTION;

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

    COMMIT;

    SELECT LAST_INSERT_ID() AS NewSettingId;
END //

DELIMITER ;
