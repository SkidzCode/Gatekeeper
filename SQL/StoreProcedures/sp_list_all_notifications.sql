DROP PROCEDURE IF EXISTS sp_list_all_notifications;
DELIMITER //
CREATE PROCEDURE sp_list_all_notifications()
BEGIN
    SELECT
        id,
        recipient_id,
        channel,
        message,
        is_sent,
        scheduled_at,
        created_at,
        updated_at
    FROM notifications;
END //
DELIMITER ;