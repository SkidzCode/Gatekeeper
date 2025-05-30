-- Stored procedure to delete notification template localizations by TemplateId
DELIMITER //

CREATE PROCEDURE NotificationTemplateLocalizationsDeleteByTemplateId(
    IN p_TemplateId INT
)
BEGIN
    DELETE FROM notification_template_localizations
    WHERE TemplateId = p_TemplateId;
END //

DELIMITER ;
