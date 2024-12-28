DROP PROCEDURE IF EXISTS sp_list_notifications_not_sent ;
DELIMITER //
CREATE PROCEDURE sp_list_notifications_not_sent(
    IN p_current_time DATETIME
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
    WHERE is_sent = FALSE
      AND (scheduled_at <= p_current_time OR scheduled_at IS NULL);
END //
DELIMITER ;