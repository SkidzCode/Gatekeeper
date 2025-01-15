DROP PROCEDURE IF EXISTS NotificationInsert ;
DELIMITER //
CREATE PROCEDURE NotificationInsert(
    IN p_RecipientId INT,
    IN p_Channel VARCHAR(10),
    IN p_URL VARCHAR(255),
    IN p_TokenType VARCHAR(255),
    IN p_Subject TEXT,
    IN p_Message TEXT,
    IN p_ScheduledAt DATETIME
)
BEGIN
    INSERT INTO Notifications (
        RecipientId,
        Channel,
        URL,
        TokenType,
        Subject,
        Message,
        ScheduledAt
    )
    VALUES (
        p_RecipientId,
        p_Channel,
        p_URL,
        p_TokenType,
        p_Subject,
        p_Message,
        p_ScheduledAt
    );

    -- Return the newly inserted ID
    SELECT LAST_INSERT_ID() AS new_id;
END //
DELIMITER ;