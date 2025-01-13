DROP PROCEDURE IF EXISTS NotificationTemplateInsert;
DELIMITER //

/* =====================
   1) INSERT
   ===================== */
CREATE PROCEDURE NotificationTemplateInsert(
    IN  p_TemplateName VARCHAR(100),
    IN  p_Channel ENUM('email', 'sms', 'push', 'inapp'),
    IN  p_TokenType VARCHAR(255),
    IN  p_Subject VARCHAR(255),
    IN  p_Body TEXT,
    IN  p_IsActive TINYINT(1)
)
BEGIN
    INSERT INTO NotificationTemplates(TemplateName, Channel, TokenType, Subject, Body, IsActive)
    VALUES (p_TemplateName, p_Channel, p_TokenType, p_Subject, p_Body, p_IsActive);

    /* Return the newly created Template ID */
    SELECT LAST_INSERT_ID() AS NewTemplateId;
END //
DELIMITER ;

DROP PROCEDURE IF EXISTS NotificationTemplateUpdate;
/* =====================
   2) UPDATE
   ===================== */
DELIMITER //
CREATE PROCEDURE NotificationTemplateUpdate(
    IN  p_TemplateId INT,
    IN  p_TemplateName VARCHAR(100),
    IN  p_Channel ENUM('email', 'sms', 'push', 'inapp'),
    IN  p_TokenType VARCHAR(255),
    IN  p_Subject VARCHAR(255),
    IN  p_Body TEXT,
    IN  p_IsActive TINYINT(1)
)
BEGIN
    UPDATE NotificationTemplates
    SET 
        TemplateName = p_TemplateName,
        Channel       = p_Channel,
        TokenType     = p_TokenType,
        Subject       = p_Subject,
        Body          = p_Body,
        IsActive     = p_IsActive,
        UpdatedAt    = CURRENT_TIMESTAMP
    WHERE TemplateId = p_TemplateId;
END //
DELIMITER ;

/* =====================
   3) DELETE
   ===================== */
DROP PROCEDURE IF EXISTS NotificationTemplateDelete;
DELIMITER //
CREATE PROCEDURE NotificationTemplateDelete(
    IN p_TemplateId INT
)
BEGIN
    DELETE FROM NotificationTemplates
    WHERE TemplateId = p_TemplateId;
END //
DELIMITER ;

/* =====================
   4) GET BY ID
   ===================== */
DROP PROCEDURE IF EXISTS NotificationTemplateGet;
DELIMITER //
CREATE PROCEDURE NotificationTemplateGet(
    IN p_TemplateId INT
)
BEGIN
    SELECT *
    FROM NotificationTemplates
    WHERE TemplateId = p_TemplateId;
END //
DELIMITER ;

/* =====================
   5) GET ALL
   ===================== */
DROP PROCEDURE IF EXISTS NotificationTemplateGetAll;
DELIMITER //
CREATE PROCEDURE NotificationTemplateGetAll()
BEGIN
    SELECT *
    FROM NotificationTemplates;
END //
DELIMITER ;
