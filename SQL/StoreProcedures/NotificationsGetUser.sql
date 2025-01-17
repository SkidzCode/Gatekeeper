DROP PROCEDURE IF EXISTS NotificationsGetUser;
DELIMITER //
CREATE PROCEDURE NotificationsGetUser(
    IN p_RecipientId INT
)
BEGIN
    SELECT
        Id,
        RecipientId,
        FromId,
        ToName,
        ToEmail,
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