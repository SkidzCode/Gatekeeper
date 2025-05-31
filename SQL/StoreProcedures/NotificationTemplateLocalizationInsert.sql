-- Stored procedure to insert a new notification template localization
DELIMITER //

CREATE PROCEDURE NotificationTemplateLocalizationInsert(
    IN p_TemplateId INT,
    IN p_LanguageCode VARCHAR(10),
    IN p_LocalizedSubject NVARCHAR(255),
    IN p_LocalizedBody TEXT
)
BEGIN
    INSERT INTO notification_template_localizations (
        TemplateId,
        LanguageCode,
        LocalizedSubject,
        LocalizedBody
    ) VALUES (
        p_TemplateId,
        p_LanguageCode,
        p_LocalizedSubject,
        p_LocalizedBody
    );

    SELECT LAST_INSERT_ID() AS LocalizationId;
END //

DELIMITER ;
