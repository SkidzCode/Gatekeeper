CREATE TABLE notifications (
    id INT AUTO_INCREMENT PRIMARY KEY, -- Unique identifier for each notification
    recipient_id INT NOT NULL,         -- Identifier for the recipient of the notification
    channel ENUM('email', 'sms', 'push', 'inapp') NOT NULL DEFAULT 'email', -- Notification channel
    message TEXT NOT NULL,             -- Content of the notification
    is_sent BOOLEAN NOT NULL DEFAULT FALSE, -- Whether the notification has been sent
    scheduled_at DATETIME NULL,        -- Optional: When the notification is scheduled to be sent
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, -- Creation timestamp
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP, -- Last updated timestamp
    INDEX (channel),                   -- Index to optimize queries filtering by channel
    INDEX (recipient_id),              -- Index to optimize queries filtering by recipient
    INDEX (is_sent)                    -- Index to optimize queries filtering by send status
);
