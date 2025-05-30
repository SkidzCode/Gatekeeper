-- Create the `Users` table to store user information
CREATE TABLE IF NOT EXISTS Users (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Username VARCHAR(50) NOT NULL UNIQUE,
    Email VARCHAR(100) NOT NULL UNIQUE,
    Password VARCHAR(255) NOT NULL,
    Salt VARCHAR(255) NOT NULL,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Phone VARCHAR(15), -- Now nullable
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- Create the `Roles` table to store user roles
CREATE TABLE IF NOT EXISTS Roles (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    RoleName VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS UserRoles (
    UserId INT NOT NULL,
    RoleId INT NOT NULL,
    PRIMARY KEY (UserId, RoleId),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);



CREATE TABLE IF NOT EXISTS AssetTypes (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(50) NOT NULL
);

CREATE TABLE IF NOT EXISTS Assets (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    AssetType INT NOT NULL,
    Asset LONGBLOB NOT NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (AssetType) REFERENCES AssetTypes(Id)
);

CREATE TABLE NotificationTemplates (
    TemplateId INT AUTO_INCREMENT PRIMARY KEY,
    TemplateName VARCHAR(100) NOT NULL,
    Channel ENUM('email', 'sms', 'push', 'inapp') NOT NULL DEFAULT 'email',
    TokenType VARCHAR(255) NULL,
    Subject VARCHAR(255) NOT NULL,
    Body TEXT NOT NULL,
    IsActive TINYINT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);


CREATE TABLE Notifications (
    Id INT AUTO_INCREMENT PRIMARY KEY, -- Unique identifier for each notification
    RecipientId INT NOT NULL,         -- Identifier for the recipient of the notification
    FromId INT NULL,
    ToName VARCHAR(255) NOT NULL,
    ToEmail VARCHAR(255) NOT NULL,
    Channel ENUM('email', 'sms', 'push', 'inapp') NOT NULL DEFAULT 'email', -- Notification channel
    URL VARCHAR(255) NOT NULL,     -- NOT Optional: URL for which the notification is intended
    TokenType VARCHAR(255) NULL,       -- Used to generate tokens for validation
    Subject TEXT NOT NULL,             -- Content of the notification
    Message TEXT NOT NULL,             -- Content of the notification
    IsSent BOOLEAN NOT NULL DEFAULT FALSE, -- Whether the notification has been sent
    ScheduledAt DATETIME NULL,        -- Optional: When the notification is scheduled to be sent
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, -- Creation timestamp
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP, -- Last updated timestamp
    INDEX (Channel),                   -- Index to optimize queries filtering by channel
    INDEX (RecipientId),              -- Index to optimize queries filtering by recipient
    INDEX (IsSent)                    -- Index to optimize queries filtering by send status
);

CREATE TABLE IF NOT EXISTS Verification (
    Id VARCHAR(36) PRIMARY KEY,
    VerifyType VARCHAR(20),
    UserId INT NOT NULL,
    HashedToken VARCHAR(255) NOT NULL,
    Salt VARCHAR(255) NOT NULL,
    ExpiryDate DATETIME NOT NULL,
    Complete BOOLEAN DEFAULT FALSE,
    Revoked BOOLEAN DEFAULT FALSE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserID) REFERENCES Users(Id)
);

CREATE TABLE IF NOT EXISTS Invites
(
    Id             INT AUTO_INCREMENT PRIMARY KEY,
    FromId         INT NOT NULL,           -- ID of the user sending the invite
    ToName         VARCHAR(255) NOT NULL,  -- Recipient name
    ToEmail        VARCHAR(255) NOT NULL,  -- Recipient email
    VerificationId CHAR(36) NOT NULL,      -- Invite token reference
    NotificationId INT,                    -- Associated notification
    Created        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	FOREIGN KEY (FromId) REFERENCES Users(Id) ON DELETE CASCADE,
	FOREIGN KEY (NotificationId) REFERENCES Notifications(Id) ON DELETE CASCADE,
	FOREIGN KEY (VerificationId) REFERENCES Verification(Id) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `KeySecrets` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `SecretKey` VARBINARY(512) NOT NULL,     -- Encrypted data
    `CreatedDate` DATETIME NOT NULL,
    `ExpirationDate` DATETIME NOT NULL,
    `IsActive` TINYINT NOT NULL DEFAULT 1, -- 1=true, 0=false
    PRIMARY KEY (`Id`)
);

CREATE TABLE IF NOT EXISTS Session (
    Id VARCHAR(36) PRIMARY KEY,
    UserId INT NOT NULL,
    VerificationId VARCHAR(36) NOT NULL,
    ExpiryDate DATETIME NOT NULL,
    Complete BOOLEAN DEFAULT FALSE,
    Revoked BOOLEAN DEFAULT FALSE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    IpAddress VARCHAR(45),
    UserAgent VARCHAR(255),
    SessionData TEXT,
    FOREIGN KEY (UserID) REFERENCES Users(Id),
    FOREIGN KEY (VerificationId) REFERENCES Verification(Id)
);

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

