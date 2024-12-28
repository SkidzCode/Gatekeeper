DROP PROCEDURE IF EXISTS sp_insert_notification ;
DELIMITER //
CREATE PROCEDURE sp_insert_notification(
    IN p_recipient_id INT,
    IN p_channel VARCHAR(10),
    IN p_message TEXT,
    IN p_scheduled_at DATETIME
)
BEGIN
    INSERT INTO notifications (
        recipient_id,
        channel,
        message,
        scheduled_at
    )
    VALUES (
        p_recipient_id,
        p_channel,
        p_message,
        p_scheduled_at
    );

    -- Return the newly inserted ID
    SELECT LAST_INSERT_ID() AS new_id;
END //
DELIMITER ;