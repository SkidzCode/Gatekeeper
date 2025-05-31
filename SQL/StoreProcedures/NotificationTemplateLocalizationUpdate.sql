-- Stored procedure to update an existing notification template localization
DELIMITER //

CREATE PROCEDURE NotificationTemplateLocalizationUpdate(
    IN p_LocalizationId INT,
    IN p_LocalizedSubject NVARCHAR(255),
    IN p_LocalizedBody TEXT
)
BEGIN
    UPDATE notification_template_localizations
    SET
        LocalizedSubject = p_LocalizedSubject,
        LocalizedBody = p_LocalizedBody,
        UpdatedAt = CURRENT_TIMESTAMP
    WHERE LocalizationId = p_LocalizationId;
END //

DELIMITER ;
