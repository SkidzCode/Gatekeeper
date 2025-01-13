DROP PROCEDURE IF EXISTS NotificationsGetNotSent ;
DELIMITER //
CREATE PROCEDURE NotificationsGetNotSent(
    IN p_current_time DATETIME
)
BEGIN
    SELECT
        Id,
        RecipientId,
        Channel,
        Subject,
        Message,
        IsSent,
        ScheduledAt,
        CreatedAt,
        UpdatedAt
    FROM Notifications
    WHERE IsSent = FALSE
      AND (ScheduledAt <= p_current_time OR ScheduledAt IS NULL);
END //
DELIMITER ;