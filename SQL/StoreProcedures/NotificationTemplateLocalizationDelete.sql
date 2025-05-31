-- Stored procedure to delete a notification template localization
DELIMITER //

CREATE PROCEDURE NotificationTemplateLocalizationDelete(
    IN p_LocalizationId INT
)
BEGIN
    DELETE FROM notification_template_localizations
    WHERE LocalizationId = p_LocalizationId;
END //

DELIMITER ;
