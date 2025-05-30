-- Stored procedure to get a notification template localization by TemplateId and LanguageCode
DELIMITER //

CREATE PROCEDURE NotificationTemplateLocalizationGetByTemplateIdAndLanguageCode(
    IN p_TemplateId INT,
    IN p_LanguageCode VARCHAR(10)
)
BEGIN
    SELECT
        LocalizationId,
        TemplateId,
        LanguageCode,
        LocalizedSubject,
        LocalizedBody,
        CreatedAt,
        UpdatedAt
    FROM notification_template_localizations
    WHERE TemplateId = p_TemplateId AND LanguageCode = p_LanguageCode;
END //

DELIMITER ;
