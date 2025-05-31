-- Defines the notification_template_localizations table
CREATE TABLE notification_template_localizations (
    LocalizationId INT AUTO_INCREMENT PRIMARY KEY,
    TemplateId INT,
    LanguageCode VARCHAR(10) NOT NULL,
    LocalizedSubject NVARCHAR(255) NULL,
    LocalizedBody TEXT NOT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT FK_NotificationTemplateLocalizations_TemplateId FOREIGN KEY (TemplateId) REFERENCES notification_templates(TemplateId) ON DELETE CASCADE,
    CONSTRAINT UQ_NotificationTemplateLocalizations_TemplateId_LanguageCode UNIQUE (TemplateId, LanguageCode)
);
