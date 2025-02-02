DROP PROCEDURE IF EXISTS InsertInvite;
DELIMITER $$
CREATE PROCEDURE InsertInvite (
    IN p_FromId INT,
    IN p_ToName VARCHAR(255),
    IN p_ToEmail VARCHAR(255),
    IN p_VerificationId CHAR(36),
    IN p_NotificationId INT,
    OUT last_id INT
)
BEGIN
    INSERT INTO Invites (
        FromId,
        ToName,
        ToEmail,
        VerificationId,
        NotificationId
    )
    VALUES (
        p_FromId,
        p_ToName,
        p_ToEmail,
        p_VerificationId,
        p_NotificationId
    );
    
    SET last_id = LAST_INSERT_ID();
END$$
DELIMITER ;
