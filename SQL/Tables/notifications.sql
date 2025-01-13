CREATE TABLE Notifications (
    Id INT AUTO_INCREMENT PRIMARY KEY, -- Unique identifier for each notification
    RecipientId INT NOT NULL,         -- Identifier for the recipient of the notification
    Channel ENUM('email', 'sms', 'push', 'inapp') NOT NULL DEFAULT 'email', -- Notification channel
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

