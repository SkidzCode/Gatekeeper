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