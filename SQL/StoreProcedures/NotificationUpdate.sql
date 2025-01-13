DROP PROCEDURE IF EXISTS NotificationUpdate;
DELIMITER //
CREATE PROCEDURE NotificationUpdate(
    IN p_NotificationId INT,
    IN p_IsSent TinyInt,
    IN p_UpdatedAt DATETIME

)
BEGIN
    UPDATE Notifications
    SET 
        IsSent = p_IsSent,
        UpdatedAt = p_UpdatedAt
    WHERE Id = p_NotificationId;

    -- Return the updated row's ID (if needed)
    SELECT p_NotificationId AS updated_id;
END //
DELIMITER ;
