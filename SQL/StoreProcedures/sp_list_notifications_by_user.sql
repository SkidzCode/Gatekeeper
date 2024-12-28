DROP PROCEDURE IF EXISTS sp_list_notifications_by_user;
DELIMITER //
CREATE PROCEDURE sp_list_notifications_by_user(
    IN p_recipient_id INT
)
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
    FROM notifications
    WHERE recipient_id = p_recipient_id;
END //
DELIMITER ;