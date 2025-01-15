DROP PROCEDURE IF EXISTS NotificationsGetUser;
DELIMITER //
CREATE PROCEDURE NotificationsGetUser(
    IN p_RecipientId INT
)
BEGIN
    SELECT
        Id,
        RecipientId,
        Channel,
        URL,
        TokenType,
        Subject,
        Message,
        IsSent,
        ScheduledAt,
        CreatedAt,
        UpdatedAt
    FROM Notifications
    WHERE RecipientId = p_RecipientId;
END //
DELIMITER ;