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
