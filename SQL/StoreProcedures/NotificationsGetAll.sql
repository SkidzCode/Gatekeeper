DROP PROCEDURE IF EXISTS NotificationsGetAll;
DELIMITER //
CREATE PROCEDURE NotificationsGetAll()
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
    FROM Notifications;
END //
DELIMITER ;