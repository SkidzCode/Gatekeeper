DROP PROCEDURE IF EXISTS spInsertNotificationTemplate;
DELIMITER //

/* =====================
   1) INSERT
   ===================== */
CREATE PROCEDURE spInsertNotificationTemplate(
    IN  p_template_name VARCHAR(100),
    IN  p_channel ENUM('email', 'sms', 'push', 'inapp'),
    IN  p_subject VARCHAR(255),
    IN  p_body TEXT,
    IN  p_is_active TINYINT(1)
)
BEGIN
    INSERT INTO notification_templates(template_name, channel, subject, body, is_active)
    VALUES (p_template_name, p_channel, p_subject, p_body, p_is_active);

    /* Return the newly created Template ID */
    SELECT LAST_INSERT_ID() AS NewTemplateId;
END //
DELIMITER ;

DROP PROCEDURE IF EXISTS spUpdateNotificationTemplate;
/* =====================
   2) UPDATE
   ===================== */
DELIMITER //
CREATE PROCEDURE spUpdateNotificationTemplate(
    IN  p_template_id INT,
    IN  p_template_name VARCHAR(100),
    IN  p_channel ENUM('email', 'sms', 'push', 'inapp'),
    IN  p_subject VARCHAR(255),
    IN  p_body TEXT,
    IN  p_is_active TINYINT(1)
)
BEGIN
    UPDATE notification_templates
    SET 
        template_name = p_template_name,
        channel       = p_channel,
        subject       = p_subject,
        body          = p_body,
        is_active     = p_is_active,
        updated_at    = CURRENT_TIMESTAMP
    WHERE template_id = p_template_id;
END //
DELIMITER ;

/* =====================
   3) DELETE
   ===================== */
DROP PROCEDURE IF EXISTS spDeleteNotificationTemplate;
DELIMITER //
CREATE PROCEDURE spDeleteNotificationTemplate(
    IN p_template_id INT
)
BEGIN
    DELETE FROM notification_templates
    WHERE template_id = p_template_id;
END //
DELIMITER ;

/* =====================
   4) GET BY ID
   ===================== */
DROP PROCEDURE IF EXISTS spGetNotificationTemplateById;
DELIMITER //
CREATE PROCEDURE spGetNotificationTemplateById(
    IN p_template_id INT
)
BEGIN
    SELECT *
    FROM notification_templates
    WHERE template_id = p_template_id;
END //
DELIMITER ;

/* =====================
   5) GET ALL
   ===================== */
DELIMITER //
CREATE PROCEDURE spGetAllNotificationTemplates()
BEGIN
    SELECT *
    FROM notification_templates;
END //
DELIMITER ;
