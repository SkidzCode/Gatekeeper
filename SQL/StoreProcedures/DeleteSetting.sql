DROP PROCEDURE IF EXISTS DeleteSetting;
DELIMITER //

CREATE PROCEDURE DeleteSetting(IN p_Id INT)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        -- Rollback in case of error
        ROLLBACK;
        SELECT 'An error occurred while deleting the setting.' AS ErrorMessage;
    END;

    START TRANSACTION;

    DELETE FROM Settings
    WHERE Id = p_Id;

    COMMIT;

    SELECT ROW_COUNT() AS RowsAffected;
END //

DELIMITER ;
